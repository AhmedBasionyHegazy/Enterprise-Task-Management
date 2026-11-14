using System;

namespace EmployeeTaskManagement.Core.Entities
{
    /// <summary>Server-side record of a refresh token. Only the SHA-256 hash of the token is stored.</summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public Guid FamilyId { get; set; }
        public DateTime SessionStartedUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public DateTime? LastUsedUtc { get; set; }
        public DateTime? RevokedUtc { get; set; }
        public string? RevokeReason { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceName { get; set; }
    }
}
