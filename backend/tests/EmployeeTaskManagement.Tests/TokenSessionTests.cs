using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EmployeeTaskManagement.API.Security;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;
using EmployeeTaskManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EmployeeTaskManagement.Tests;

public class TokenSessionTests
{
    private static (ApplicationDbContext Db, HttpContextAccessor Accessor, TokenSessionService Service) CreateSut(string dbName)
    {
        var accessor = new HttpContextAccessor();
        var resolver = new UserResolverService(accessor);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new ApplicationDbContext(options, resolver);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RefreshTokenDays"] = "7",
                ["Jwt:RefreshGraceSeconds"] = "10",
            })
            .Build();

        var jwtMock = new Mock<IJwtTokenGenerator>();
        var auditMock = new Mock<IAuditService>();

        var service = new TokenSessionService(
            db,
            jwtMock.Object,
            config,
            resolver,
            auditMock.Object,
            NullLogger<TokenSessionService>.Instance);

        return (db, accessor, service);
    }

    private static DefaultHttpContext CreateHttpContext(string userAgent, string ipAddress)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = userAgent;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        return context;
    }

    [Fact]
    public async Task GetSessionsAsync_ReturnsAllConcurrentActiveSessionsForUser_WithLastUsedPopulatedAndAccurateIsCurrent()
    {
        var (db, accessor, service) = CreateSut(nameof(GetSessionsAsync_ReturnsAllConcurrentActiveSessionsForUser_WithLastUsedPopulatedAndAccurateIsCurrent));

        var user1 = new User { Id = 10, Username = "ahmed", Email = "ahmed@example.com", FullName = "Ahmed", RoleId = 2 };
        var user2 = new User { Id = 20, Username = "other", Email = "other@example.com", FullName = "Other", RoleId = 2 };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        // Session 1: Desktop Edge
        accessor.HttpContext = CreateHttpContext(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/130.0.0.0",
            "10.0.0.10");
        var (desktopToken, _) = await service.IssueRefreshTokenAsync(user1.Id);

        // Session 2: iPhone Safari
        accessor.HttpContext = CreateHttpContext(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1",
            "10.0.0.25");
        var (mobileToken, _) = await service.IssueRefreshTokenAsync(user1.Id);

        // Unrelated user's active session (must never leak)
        accessor.HttpContext = CreateHttpContext("Mozilla/5.0 Chrome/130.0.0.0", "10.0.0.99");
        await service.IssueRefreshTokenAsync(user2.Id);

        var desktopSessions = await service.GetSessionsAsync(user1.Id, desktopToken);

        Assert.Equal(2, desktopSessions.Count);
        Assert.All(desktopSessions, s => Assert.NotNull(s.LastUsedUtc));

        Assert.Contains(desktopSessions, s => s.IsCurrent && s.IpAddress == "10.0.0.10");
        Assert.Contains(desktopSessions, s => !s.IsCurrent && s.IpAddress == "10.0.0.25");
        Assert.DoesNotContain(desktopSessions, s => s.IpAddress == "10.0.0.99");

        // Flip caller perspective to mobile session
        var mobileSessions = await service.GetSessionsAsync(user1.Id, mobileToken);
        Assert.Equal(2, mobileSessions.Count);
        Assert.Contains(mobileSessions, s => s.IsCurrent && s.IpAddress == "10.0.0.25");
        Assert.Contains(mobileSessions, s => !s.IsCurrent && s.IpAddress == "10.0.0.10");

        // Without refresh token cookie, still returns all active sessions with IsCurrent = false
        var unauthenticatedView = await service.GetSessionsAsync(user1.Id, null);
        Assert.Equal(2, unauthenticatedView.Count);
        Assert.All(unauthenticatedView, s => Assert.False(s.IsCurrent));
    }

    [Fact]
    public async Task GetSessionsAsync_ExcludesRevokedAndExpiredRowsAndDeduplicatesSameFamily()
    {
        var (db, accessor, service) = CreateSut(nameof(GetSessionsAsync_ExcludesRevokedAndExpiredRowsAndDeduplicatesSameFamily));

        var user = new User { Id = 15, Username = "sara", Email = "sara@example.com", FullName = "Sara", RoleId = 2 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        accessor.HttpContext = CreateHttpContext("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Edg/130.0.0.0", "192.168.1.5");
        var (activeToken1, _) = await service.IssueRefreshTokenAsync(user.Id);

        accessor.HttpContext = CreateHttpContext("Mozilla/5.0 (X11; Linux x86_64) Firefox/132.0", "192.168.1.6");
        await service.IssueRefreshTokenAsync(user.Id);

        var row1 = await db.RefreshTokens.SingleAsync(t => t.UserId == user.Id && t.IpAddress == "192.168.1.5");

        // Older rotated predecessor in the same family, plus an already-revoked logout token, plus an expired token, plus a defensive duplicate in same family
        var now = DateTime.UtcNow;
        db.RefreshTokens.AddRange(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "predecessor-hash",
                FamilyId = row1.FamilyId,
                SessionStartedUtc = row1.SessionStartedUtc,
                CreatedUtc = row1.CreatedUtc.AddMinutes(-5),
                LastUsedUtc = row1.CreatedUtc.AddMinutes(-5),
                ExpiresUtc = row1.ExpiresUtc,
                RevokedUtc = now.AddMinutes(-4),
                RevokeReason = "Rotated",
                ReplacedByTokenHash = row1.TokenHash,
                IpAddress = "192.168.1.5",
                DeviceName = row1.DeviceName,
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "duplicate-family-active-hash",
                FamilyId = row1.FamilyId,
                SessionStartedUtc = row1.SessionStartedUtc,
                CreatedUtc = row1.CreatedUtc.AddMinutes(-2),
                LastUsedUtc = row1.CreatedUtc.AddMinutes(-2),
                ExpiresUtc = row1.ExpiresUtc,
                IpAddress = "192.168.1.5",
                DeviceName = row1.DeviceName,
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "logged-out-hash",
                FamilyId = Guid.NewGuid(),
                SessionStartedUtc = now.AddHours(-2),
                CreatedUtc = now.AddHours(-2),
                LastUsedUtc = now.AddHours(-1),
                ExpiresUtc = now.AddDays(5),
                RevokedUtc = now.AddMinutes(-30),
                RevokeReason = "Logout",
                IpAddress = "192.168.1.7",
                DeviceName = "Revoked Device",
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "expired-hash",
                FamilyId = Guid.NewGuid(),
                SessionStartedUtc = now.AddDays(-10),
                CreatedUtc = now.AddDays(-10),
                LastUsedUtc = now.AddDays(-10),
                ExpiresUtc = now.AddMinutes(-1),
                IpAddress = "192.168.1.8",
                DeviceName = "Expired Device",
            });
        await db.SaveChangesAsync();

        var sessions = await service.GetSessionsAsync(user.Id, activeToken1);

        Assert.Equal(2, sessions.Count);
        Assert.DoesNotContain(sessions, s => s.IpAddress == "192.168.1.7" || s.IpAddress == "192.168.1.8");
        Assert.Single(sessions, s => s.IpAddress == "192.168.1.5");
        Assert.Contains(sessions, s => s.Id == row1.Id && s.IsCurrent);
    }

    [Fact]
    public void AuthCookieService_RefreshPath_CoversBothLowercaseAndSwaggerAuthRoutes()
    {
        Assert.Equal("/api", AuthCookieService.RefreshPath);

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Development");
        var cookies = new AuthCookieService(new ConfigurationBuilder().Build(), env.Object);

        Assert.NotNull(cookies);
        Assert.StartsWith(AuthCookieService.RefreshPath, "/api/Auth/sessions", StringComparison.Ordinal);
        Assert.StartsWith(AuthCookieService.RefreshPath, "/api/auth/sessions", StringComparison.Ordinal);
    }
}
