
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EmployeeTaskManagement.Application.Services
{
    public class UserResolverService
    {
        private readonly IHttpContextAccessor _context;

        public UserResolverService(IHttpContextAccessor context)
        {
            _context = context;
        }

        public string? GetUser()
        {
            var httpContext = _context.HttpContext;

            if (httpContext == null)
                return null;

            // Prefer authenticated user's identifier.
            var userId =
                httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User?.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
                return userId;

            // Fallback to authenticated identity name.
            return httpContext.User?.Identity?.Name;
        }
    }
}
