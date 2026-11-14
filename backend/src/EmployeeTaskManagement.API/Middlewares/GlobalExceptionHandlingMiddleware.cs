using System.Net;
using System.Text.Json;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.Services;

namespace EmployeeTaskManagement.API.Middlewares
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path.Value);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted) return Task.CompletedTask;

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var correlationId = context.Items.TryGetValue(UserResolverService.CorrelationItemKey, out var c) ? c as string : null;
            // Internal details only in Development; production clients get the correlation id to quote to support.
            var errors = new List<string>();
            if (_env.IsDevelopment()) errors.Add(exception.Message);
            if (correlationId != null) errors.Add($"correlationId:{correlationId}");

            var response = ApiResponse<object>.Fail(
                message: "An internal server error occurred. Please contact system support.",
                errors: errors,
                statusCode: (int)HttpStatusCode.InternalServerError);

            return context.Response.WriteAsync(JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
