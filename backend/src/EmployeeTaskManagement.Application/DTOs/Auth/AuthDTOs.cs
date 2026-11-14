using System;
using System.Text.Json.Serialization;
using EmployeeTaskManagement.Application.DTOs.Users;

namespace EmployeeTaskManagement.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        /// <summary>
        /// Access token. NEVER serialized to API clients: it is delivered only through an HTTP-only cookie.
        /// </summary>
        [JsonIgnore]
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class SessionDto
    {
        public Guid Id { get; set; }
        public string? DeviceName { get; set; }
        public string? IpAddress { get; set; }
        public DateTime SessionStartedUtc { get; set; }
        public DateTime? LastUsedUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public bool IsCurrent { get; set; }
    }
}
