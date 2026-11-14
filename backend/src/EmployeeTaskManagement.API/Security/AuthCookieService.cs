using EmployeeTaskManagement.Application.Interfaces.Security;
using System.Security.Cryptography;

namespace EmployeeTaskManagement.API.Security
{
    /// <summary>Single place that knows how authentication cookies are written, rotated and cleared.</summary>
    public class AuthCookieService
    {
        public const string AccessCookie = "tf_access";
        public const string RefreshCookie = "tf_refresh";
        public const string CsrfCookie = "tf_csrf";
        public const string CsrfHeader = "X-CSRF-TOKEN";
        // Scope under "/api" rather than "/api/auth" so mixed-case routes (e.g. Swagger's /api/Auth/*) still receive the cookie.
        public const string RefreshPath = "/api";
        private const string LegacyRefreshPath = "/api/auth";

        private readonly SameSiteMode _sameSite;
        private readonly bool _secure;
        private readonly string? _domain;
        private readonly int _refreshDays;

        public AuthCookieService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _sameSite = Enum.TryParse<SameSiteMode>(configuration["Cookies:SameSite"], true, out var s) ? s : SameSiteMode.Lax;
            // Secure is mandatory outside Development (SameSite=None also requires it).
            _secure = bool.TryParse(configuration["Cookies:Secure"], out var sec) ? sec : !env.IsDevelopment();
            if (_sameSite == SameSiteMode.None) _secure = true;
            _domain = string.IsNullOrWhiteSpace(configuration["Cookies:Domain"]) ? null : configuration["Cookies:Domain"];
            _refreshDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var d) && d > 0 ? d : 7;
        }

        private CookieOptions Options(DateTime? expiresUtc, string path) => new()
        {
            HttpOnly = true,                 // never readable by JavaScript
            Secure = _secure,
            SameSite = _sameSite,
            Path = path,
            Domain = _domain,
            IsEssential = true,
            Expires = expiresUtc.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(expiresUtc.Value, DateTimeKind.Utc)) : null
        };

        public void WriteSession(HttpContext ctx, SessionTokens tokens)
        {
            ctx.Response.Cookies.Append(AccessCookie, tokens.AccessToken, Options(tokens.AccessExpiresUtc, "/"));
            ctx.Response.Cookies.Append(RefreshCookie, tokens.RefreshToken, Options(tokens.RefreshExpiresUtc, RefreshPath));
        }

        public void WriteRefresh(HttpContext ctx, string token, DateTime expiresUtc) =>
            ctx.Response.Cookies.Append(RefreshCookie, token, Options(expiresUtc, RefreshPath));

        public void WriteAccess(HttpContext ctx, string token, DateTime expiresUtc) =>
            ctx.Response.Cookies.Append(AccessCookie, token, Options(expiresUtc, "/"));

        /// <summary>Returns the CSRF token bound to this browser, creating (or rotating) the cookie when needed.</summary>
        public string EnsureCsrf(HttpContext ctx, bool rotate = false)
        {
            var existing = ctx.Request.Cookies[CsrfCookie];
            if (!rotate && !string.IsNullOrEmpty(existing)) return existing;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            ctx.Response.Cookies.Append(CsrfCookie, token, Options(DateTime.UtcNow.AddDays(_refreshDays), "/"));
            return token;
        }

        public void Clear(HttpContext ctx)
        {
            ctx.Response.Cookies.Delete(AccessCookie, Options(null, "/"));
            ctx.Response.Cookies.Delete(RefreshCookie, Options(null, RefreshPath));
            ctx.Response.Cookies.Delete(RefreshCookie, Options(null, LegacyRefreshPath));
            ctx.Response.Cookies.Delete(CsrfCookie, Options(null, "/"));
        }
    }
}
