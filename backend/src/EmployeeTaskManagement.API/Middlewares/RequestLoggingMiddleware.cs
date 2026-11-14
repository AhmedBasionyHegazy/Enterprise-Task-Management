using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using EmployeeTaskManagement.Application.Services;

namespace EmployeeTaskManagement.API.Middlewares
{
    /// <summary>
    /// Establishes the correlation id for the request, opens a logging scope carrying the shared
    /// structured fields and logs one summary line per request (never bodies, headers or cookies).
    /// </summary>
    public partial class RequestLoggingMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        [GeneratedRegex(@"^[A-Za-z0-9\-_.]{8,64}$")] private static partial Regex ValidId();

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var incoming = context.Request.Headers[HeaderName].ToString();
            var correlationId = ValidId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");
            context.Items[UserResolverService.CorrelationItemKey] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            var scope = new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = traceId,
                ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            };

            using (_logger.BeginScope(scope))
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await _next(context);
                }
                finally
                {
                    sw.Stop();
                    var status = context.Response.StatusCode;
                    var level = status >= 500 ? LogLevel.Error : status >= 400 ? LogLevel.Warning : LogLevel.Information;
                    var ua = context.Request.Headers["User-Agent"].ToString();
                    var safeUa = ua.Length > 200 ? ua[..200] : ua;
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                    var username = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "anonymous";

                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        ["UserId"] = userId,
                        ["Username"] = username,
                        ["UserAgent"] = safeUa
                    }))
                    {
                        _logger.Log(level,
                            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms user={UserId}",
                            context.Request.Method,
                            context.Request.Path.Value,       // query string deliberately omitted
                            status,
                            sw.ElapsedMilliseconds,
                            userId);
                    }
                }
            }
        }
    }
}
