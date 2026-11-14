using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EmployeeTaskManagement.Application.Services
{
    /// <summary>Resolves the current request's identity and request metadata (used by audit + logging).</summary>
    public class UserResolverService
    {
        public const string CorrelationItemKey = "CorrelationId";
        private readonly IHttpContextAccessor _context;

        public UserResolverService(IHttpContextAccessor context)
        {
            _context = context;
        }

        public string? GetUser()
        {
            var httpContext = _context.HttpContext;
            if (httpContext == null) return null;

            var userId =
                httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User?.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(userId)) return userId;
            return httpContext.User?.Identity?.Name;
        }

        public int? GetUserIdInt() => int.TryParse(GetUser(), out var id) ? id : null;

        public string? GetUsername() =>
            _context.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

        public string? GetIpAddress() =>
            Truncate(_context.HttpContext?.Connection?.RemoteIpAddress?.ToString(), 64);

        public string? GetUserAgent() =>
            Truncate(_context.HttpContext?.Request?.Headers["User-Agent"].ToString(), 400);

        public string? GetCorrelationId()
        {
            var ctx = _context.HttpContext;
            if (ctx == null) return null;
            return ctx.Items.TryGetValue(CorrelationItemKey, out var v) && v is string s ? s : ctx.TraceIdentifier;
        }

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) ? null : (value.Length <= max ? value : value[..max]);
    }
}
