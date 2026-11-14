using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using EmployeeTaskManagement.API.Security;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Auth;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Enums;

namespace EmployeeTaskManagement.API.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly ITokenSessionService _sessions;
        private readonly AuthCookieService _cookies;
        private readonly IAuditService _audit;
        private readonly IMapper _mapper;

        public AuthController(IAuthService authService, ITokenSessionService sessions, AuthCookieService cookies, IAuditService audit, IMapper mapper)
        {
            _authService = authService;
            _sessions = sessions;
            _cookies = cookies;
            _audit = audit;
            _mapper = mapper;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }

            var (refresh, refreshExpires) = await _sessions.IssueRefreshTokenAsync(result.Data!.User.Id);
            _cookies.WriteSession(HttpContext, new SessionTokens(result.Data.Token, result.Data.ExpiresAt, refresh, refreshExpires));
            _cookies.EnsureCsrf(HttpContext, rotate: true);

            return Ok(result);   // Token is [JsonIgnore]: tokens only ever travel in HttpOnly cookies
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Refresh()
        {
            var raw = Request.Cookies[AuthCookieService.RefreshCookie];
            if (string.IsNullOrEmpty(raw))
            {
                return StatusCode(401, ApiResponse<AuthResponseDto>.Fail("No active session.", statusCode: 401));
            }

            var rotated = await _sessions.RotateAsync(raw);
            switch (rotated.Status)
            {
                case RefreshStatus.Success:
                    _cookies.WriteSession(HttpContext, rotated.Tokens!);
                    return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
                    {
                        ExpiresAt = rotated.Tokens!.AccessExpiresUtc,
                        User = _mapper.Map<UserDto>(rotated.User)
                    }, "Session refreshed."));

                case RefreshStatus.Conflict:
                    // Another request rotated this token a moment ago; the caller should retry with the new cookies.
                    return StatusCode(409, ApiResponse<AuthResponseDto>.Fail("Refresh already in progress.", statusCode: 409));

                default:
                    _cookies.Clear(HttpContext);
                    return StatusCode(401, ApiResponse<AuthResponseDto>.Fail("Session expired. Please sign in again.", statusCode: 401));
            }
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            var raw = Request.Cookies[AuthCookieService.RefreshCookie];
            if (!string.IsNullOrEmpty(raw))
            {
                var (userId, username) = await _sessions.RevokeByTokenAsync(raw, "Logout");
                if (userId.HasValue)
                    await _audit.LogAsync(AuditActions.Logout, "Auth", userId.ToString(), true, "User signed out.", userId, username);
            }
            _cookies.Clear(HttpContext);
            return Ok(ApiResponse<bool>.Ok(true, "Signed out."));
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            var count = await _sessions.RevokeAllForUserAsync(CurrentUserId, "LogoutAll");
            await _audit.LogAsync(AuditActions.LogoutAll, "Auth", CurrentUserId.ToString(), true, $"Signed out of all devices ({count} session(s) revoked).");
            _cookies.Clear(HttpContext);
            return Ok(ApiResponse<int>.Ok(count, "Signed out of all devices."));
        }

        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> GetSessions()
        {
            var list = await _sessions.GetSessionsAsync(CurrentUserId, Request.Cookies[AuthCookieService.RefreshCookie]);
            return Ok(ApiResponse<IReadOnlyList<SessionDto>>.Ok(list));
        }

        [HttpDelete("sessions/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> RevokeSession(Guid id)
        {
            var ok = await _sessions.RevokeSessionAsync(CurrentUserId, id);
            if (!ok) return NotFound(ApiResponse<bool>.Fail("Session not found.", statusCode: 404));

            await _audit.LogAsync(AuditActions.SessionRevoked, "Auth", CurrentUserId.ToString(), true, "A session was revoked.");
            return Ok(ApiResponse<bool>.Ok(true, "Session revoked."));
        }

        [HttpGet("csrf")]
        [AllowAnonymous]
        public IActionResult Csrf() => Ok(ApiResponse<string>.Ok(_cookies.EnsureCsrf(HttpContext)));

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var result = await _authService.GetCurrentUserAsync(CurrentUserId);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
