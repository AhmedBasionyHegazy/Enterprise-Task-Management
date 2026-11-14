using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using EmployeeTaskManagement.Infrastructure.Data;

namespace EmployeeTaskManagement.Infrastructure.Security
{
    public class TokenSessionService : ITokenSessionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IJwtTokenGenerator _jwt;
        private readonly UserResolverService _resolver;
        private readonly IAuditService _audit;
        private readonly ILogger<TokenSessionService> _logger;
        private readonly int _refreshDays;
        private readonly int _graceSeconds;

        public TokenSessionService(
            ApplicationDbContext db,
            IJwtTokenGenerator jwt,
            IConfiguration configuration,
            UserResolverService resolver,
            IAuditService audit,
            ILogger<TokenSessionService> logger)
        {
            _db = db;
            _jwt = jwt;
            _resolver = resolver;
            _audit = audit;
            _logger = logger;
            _refreshDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var d) && d > 0 ? d : 7;
            _graceSeconds = int.TryParse(configuration["Jwt:RefreshGraceSeconds"], out var g) && g >= 0 ? g : 10;
        }

        public static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        private static string NewRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        public async Task<(string Token, DateTime ExpiresUtc)> IssueRefreshTokenAsync(int userId)
        {
            var now = DateTime.UtcNow;
            var raw = NewRawToken();
            var entity = NewEntity(userId, Hash(raw), Guid.NewGuid(), now, now);
            _db.RefreshTokens.Add(entity);
            await _db.SaveChangesAsync();
            return (raw, entity.ExpiresUtc);
        }

        public async Task<RefreshResult> RotateAsync(string rawRefreshToken)
        {
            var now = DateTime.UtcNow;
            var hash = Hash(rawRefreshToken);
            var token = await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash);

            if (token == null)
                return new RefreshResult { Status = RefreshStatus.Invalid };

            if (token.RevokedUtc != null)
            {
                // Two tabs racing with the same cookie: tolerate within a short grace window, never revoke.
                if (token.RevokeReason == "Rotated" && (now - token.RevokedUtc.Value).TotalSeconds <= _graceSeconds)
                    return new RefreshResult { Status = RefreshStatus.Conflict };

                // A revoked/rotated token is being replayed => assume theft and burn the whole family.
                await RevokeFamilyAsync(token.FamilyId, "ReuseDetected", now);
                var u = await _db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == token.UserId);
                _logger.LogWarning("Refresh token reuse detected for user {UserId}; token family {FamilyId} revoked", token.UserId, token.FamilyId);
                await _audit.LogAsync(AuditActions.TokenReuse, "Auth", token.UserId.ToString(), false,
                    "A revoked refresh token was reused. All sessions of this token family were revoked.", token.UserId, u?.Username);
                return new RefreshResult { Status = RefreshStatus.ReuseDetected };
            }

            if (token.ExpiresUtc <= now)
                return new RefreshResult { Status = RefreshStatus.Invalid };

            var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == token.UserId);
            if (user == null || !user.IsActive)
            {
                await RevokeFamilyAsync(token.FamilyId, "UserInactive", now);
                return new RefreshResult { Status = RefreshStatus.Invalid };
            }

            var newRaw = NewRawToken();
            var newHash = Hash(newRaw);

            // Atomic compare-and-swap: only one concurrent caller can rotate a given token.
            var rows = await _db.RefreshTokens
                .Where(t => t.Id == token.Id && t.RevokedUtc == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.RevokedUtc, now)
                    .SetProperty(t => t.RevokeReason, "Rotated")
                    .SetProperty(t => t.ReplacedByTokenHash, newHash)
                    .SetProperty(t => t.LastUsedUtc, now));

            if (rows == 0)
                return new RefreshResult { Status = RefreshStatus.Conflict };

            var replacement = NewEntity(user.Id, newHash, token.FamilyId, token.SessionStartedUtc, now);
            replacement.LastUsedUtc = now;
            _db.RefreshTokens.Add(replacement);
            await _db.SaveChangesAsync();

            var roleName = user.Role?.Name ?? RoleNames.Employee;
            var (access, accessExp) = _jwt.GenerateToken(user, roleName);

            return new RefreshResult
            {
                Status = RefreshStatus.Success,
                User = user,
                Tokens = new SessionTokens(access, accessExp, newRaw, replacement.ExpiresUtc)
            };
        }

        public async Task<(int? UserId, string? Username)> RevokeByTokenAsync(string rawRefreshToken, string reason)
        {
            var hash = Hash(rawRefreshToken);
            var token = await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (token == null) return (null, null);

            await RevokeFamilyAsync(token.FamilyId, reason, DateTime.UtcNow);
            var user = await _db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(u => u.Id == token.UserId);
            return (token.UserId, user?.Username);
        }

        public Task<int> RevokeAllForUserAsync(int userId, string reason)
        {
            var now = DateTime.UtcNow;
            return _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedUtc, now).SetProperty(t => t.RevokeReason, reason));
        }

        public async Task<IReadOnlyList<SessionDto>> GetSessionsAsync(int userId, string? currentRawRefreshToken)
        {
            var now = DateTime.UtcNow;
            Guid? currentFamily = null;
            if (!string.IsNullOrEmpty(currentRawRefreshToken))
            {
                var h = Hash(currentRawRefreshToken);
                currentFamily = await _db.RefreshTokens.AsNoTracking().Where(t => t.TokenHash == h).Select(t => (Guid?)t.FamilyId).FirstOrDefaultAsync();
            }

            var active = await _db.RefreshTokens.AsNoTracking()
                .Where(t => t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > now)
                .OrderByDescending(t => t.LastUsedUtc ?? t.CreatedUtc)
                .ToListAsync();

            return active.Select(t => new SessionDto
            {
                Id = t.Id,
                DeviceName = t.DeviceName,
                IpAddress = t.IpAddress,
                SessionStartedUtc = DateTime.SpecifyKind(t.SessionStartedUtc, DateTimeKind.Utc),
                LastUsedUtc = t.LastUsedUtc.HasValue ? DateTime.SpecifyKind(t.LastUsedUtc.Value, DateTimeKind.Utc) : null,
                ExpiresUtc = DateTime.SpecifyKind(t.ExpiresUtc, DateTimeKind.Utc),
                IsCurrent = currentFamily.HasValue && t.FamilyId == currentFamily.Value
            }).ToList();
        }

        public async Task<bool> RevokeSessionAsync(int userId, Guid sessionId)
        {
            var token = await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId);
            if (token == null) return false;
            await RevokeFamilyAsync(token.FamilyId, "SessionRevoked", DateTime.UtcNow);
            return true;
        }

        public async Task<int> CleanupAsync(int retentionDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retentionDays));
            return await _db.RefreshTokens
                .Where(t => t.ExpiresUtc < cutoff || (t.RevokedUtc != null && t.RevokedUtc < cutoff))
                .ExecuteDeleteAsync();
        }

        private Task<int> RevokeFamilyAsync(Guid familyId, string reason, DateTime now) =>
            _db.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedUtc, now).SetProperty(t => t.RevokeReason, reason));

        private RefreshToken NewEntity(int userId, string hash, Guid family, DateTime sessionStarted, DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            FamilyId = family,
            SessionStartedUtc = sessionStarted,
            CreatedUtc = now,
            ExpiresUtc = now.AddDays(_refreshDays),
            IpAddress = _resolver.GetIpAddress(),
            UserAgent = _resolver.GetUserAgent(),
            DeviceName = DescribeDevice(_resolver.GetUserAgent())
        };

        private static string DescribeDevice(string? ua)
        {
            if (string.IsNullOrWhiteSpace(ua)) return "Unknown device";
            var browser = ua.Contains("Edg/") ? "Edge" : ua.Contains("OPR/") ? "Opera" : ua.Contains("Chrome/") ? "Chrome"
                : ua.Contains("Firefox/") ? "Firefox" : ua.Contains("Safari/") ? "Safari" : "Browser";
            var os = ua.Contains("Windows") ? "Windows" : ua.Contains("Android") ? "Android"
                : ua.Contains("iPhone") || ua.Contains("iPad") ? "iOS" : ua.Contains("Mac OS") ? "macOS"
                : ua.Contains("Linux") ? "Linux" : "Unknown OS";
            return $"{browser} on {os}";
        }
    }
}
