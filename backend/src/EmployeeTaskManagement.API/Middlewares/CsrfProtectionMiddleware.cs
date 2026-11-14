using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EmployeeTaskManagement.API.Security;
using EmployeeTaskManagement.Application.Common;

namespace EmployeeTaskManagement.API.Middlewares
{
    /// <summary>
    /// Double-submit CSRF protection for cookie-authenticated, state-changing requests.
    /// The csrf cookie is HttpOnly; the SPA obtains the value from GET /api/auth/csrf and echoes it in
    /// the X-CSRF-TOKEN header. A cross-site attacker can neither read the value nor set the header.
    /// Requests authenticated with an Authorization header (API integrations / Swagger) are not affected.
    /// </summary>
    public class CsrfProtectionMiddleware
    {
        private static readonly HashSet<string> Safe = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };
        private readonly RequestDelegate _next;

        public CsrfProtectionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var usesCookieAuth =
                context.Request.Cookies.ContainsKey(AuthCookieService.AccessCookie) ||
                context.Request.Cookies.ContainsKey(AuthCookieService.RefreshCookie);

            var exempt =
                path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||   // no session yet
                path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase);          // negotiate only; origin-checked in the hub

            if (!Safe.Contains(context.Request.Method) && usesCookieAuth && !exempt
                && !context.Request.Headers.ContainsKey("Authorization"))
            {
                var cookie = context.Request.Cookies[AuthCookieService.CsrfCookie] ?? string.Empty;
                var header = context.Request.Headers[AuthCookieService.CsrfHeader].ToString();

                var valid = cookie.Length > 0 && header.Length == cookie.Length &&
                            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookie), Encoding.UTF8.GetBytes(header));
                if (!valid)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    var body = ApiResponse<object>.Fail("CSRF validation failed.", new List<string> { "csrf" }, 403);
                    await context.Response.WriteAsync(JsonSerializer.Serialize(body,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                    return;
                }
            }

            await _next(context);
        }
    }
}
