using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Core.Entities;

namespace EmployeeTaskManagement.Application.Interfaces.Security
{
    public enum RefreshStatus { Success, Invalid, Conflict, ReuseDetected }

    public record SessionTokens(string AccessToken, DateTime AccessExpiresUtc, string RefreshToken, DateTime RefreshExpiresUtc);

    public class RefreshResult
    {
        public RefreshStatus Status { get; set; }
        public SessionTokens? Tokens { get; set; }
        public User? User { get; set; }
    }

    public interface ITokenSessionService
    {
        /// <summary>Starts a new session (new token family) and returns the raw refresh token (never stored).</summary>
        Task<(string Token, DateTime ExpiresUtc)> IssueRefreshTokenAsync(int userId);

        /// <summary>Validates + rotates a refresh token. Detects reuse and revokes the whole family.</summary>
        Task<RefreshResult> RotateAsync(string rawRefreshToken);

        /// <summary>Revokes the session (token family) the token belongs to.</summary>
        Task<(int? UserId, string? Username)> RevokeByTokenAsync(string rawRefreshToken, string reason);

        Task<int> RevokeAllForUserAsync(int userId, string reason);
        Task<IReadOnlyList<SessionDto>> GetSessionsAsync(int userId, string? currentRawRefreshToken);
        Task<bool> RevokeSessionAsync(int userId, Guid sessionId);
        Task<int> CleanupAsync(int retentionDays);
    }
}
