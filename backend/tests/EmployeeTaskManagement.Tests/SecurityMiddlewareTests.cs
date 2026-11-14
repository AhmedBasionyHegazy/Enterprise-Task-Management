using EmployeeTaskManagement.API.Logging;
using EmployeeTaskManagement.API.Middlewares;
using EmployeeTaskManagement.API.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EmployeeTaskManagement.Tests
{
    public class SecurityMiddlewareTests
    {
        private sealed class FakeEnv(string envName) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = envName;
            public string ApplicationName { get; set; } = "Test";
            public string ContentRootPath { get; set; } = Path.GetTempPath();
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        [Fact]
        public async Task CsrfMiddleware_BlocksCookiePostWithoutHeader_AllowsMatchingTokenOrBearer()
        {
            var nextCalled = false;
            var middleware = new CsrfProtectionMiddleware(_ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            // 1. Cookie auth POST without X-CSRF-TOKEN -> 403
            var badCtx = new DefaultHttpContext();
            badCtx.Request.Method = "POST";
            badCtx.Request.Path = "/api/tasks";
            badCtx.Request.Headers.Cookie = $"{AuthCookieService.AccessCookie}=jwt; {AuthCookieService.CsrfCookie}=secret-csrf";
            await middleware.InvokeAsync(badCtx);
            Assert.Equal(403, badCtx.Response.StatusCode);
            Assert.False(nextCalled);

            // 2. Cookie auth POST with valid double-submit header -> allowed
            var goodCtx = new DefaultHttpContext();
            goodCtx.Request.Method = "POST";
            goodCtx.Request.Path = "/api/tasks";
            goodCtx.Request.Headers.Cookie = $"{AuthCookieService.AccessCookie}=jwt; {AuthCookieService.CsrfCookie}=secret-csrf";
            goodCtx.Request.Headers[AuthCookieService.CsrfHeader] = "secret-csrf";
            await middleware.InvokeAsync(goodCtx);
            Assert.True(nextCalled);

            // 3. Authorization Bearer header POST without CSRF -> allowed (API clients / Swagger)
            nextCalled = false;
            var apiCtx = new DefaultHttpContext();
            apiCtx.Request.Method = "POST";
            apiCtx.Request.Path = "/api/tasks";
            apiCtx.Request.Headers.Cookie = $"{AuthCookieService.CsrfCookie}=secret-csrf";
            apiCtx.Request.Headers.Authorization = "Bearer abc.def.ghi";
            await middleware.InvokeAsync(apiCtx);
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task GlobalExceptionMiddleware_HidesExceptionMessageInProduction()
        {
            var prodMiddleware = new GlobalExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("SECRET_SQL_CONNECTION_DETAIL"),
                NullLogger<GlobalExceptionHandlingMiddleware>.Instance,
                new FakeEnv(Environments.Production));

            var ctx = new DefaultHttpContext();
            ctx.Items["CorrelationId"] = "corr-123";
            ctx.Response.Body = new MemoryStream();
            await prodMiddleware.InvokeAsync(ctx);

            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();

            Assert.Equal(500, ctx.Response.StatusCode);
            Assert.DoesNotContain("SECRET_SQL_CONNECTION_DETAIL", body);
            Assert.Contains("corr-123", body);
        }

        [Fact]
        public async Task RequestLoggingMiddleware_PreservesCorrelationIdHeader()
        {
            var middleware = new RequestLoggingMiddleware(
                _ => Task.CompletedTask,
                NullLogger<RequestLoggingMiddleware>.Instance);

            var ctx = new DefaultHttpContext();
            ctx.Request.Method = "GET";
            ctx.Request.Path = "/api/tasks";
            ctx.Request.Headers[RequestLoggingMiddleware.HeaderName] = "client-corr-id-987654";

            await middleware.InvokeAsync(ctx);

            Assert.Equal("client-corr-id-987654", ctx.Response.Headers[RequestLoggingMiddleware.HeaderName].ToString());
        }

        [Fact]
        public void FileLogger_RedactsSecretsAndJwtTokens()
        {
            var dir = Path.Combine(Path.GetTempPath(), "tf-log-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var provider = new FileLoggerProvider(new FileLoggingOptions { Enabled = true, Directory = dir }, dir))
                {
                    var logger = provider.CreateLogger("SecurityTest");
                    logger.LogInformation("User signed in token=eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.sig123 password=MySecretPassword!");
                }

                var file = Directory.EnumerateFiles(dir, "app-*.log").Single();
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("MySecretPassword!", content);
                Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9", content);
                Assert.Contains("[redacted]", content);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void AuthResponseDto_JsonSerialization_NeverIncludesRawAccessToken()
        {
            var dto = new EmployeeTaskManagement.Application.DTOs.Auth.AuthResponseDto
            {
                Token = "secret.jwt.token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(dto);
            Assert.DoesNotContain("secret.jwt.token", json);
            Assert.DoesNotContain("\"Token\"", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
