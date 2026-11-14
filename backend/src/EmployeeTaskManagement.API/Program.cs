
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using EmployeeTaskManagement.API.BackgroundServices;
using EmployeeTaskManagement.API.Hubs;
using EmployeeTaskManagement.API.Logging;
using EmployeeTaskManagement.API.Middlewares;
using EmployeeTaskManagement.API.Security;
using EmployeeTaskManagement.API.Services;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using EmployeeTaskManagement.Application.Interfaces.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Application.Mappings;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Infrastructure.Caching;
using EmployeeTaskManagement.Infrastructure.Data;
using EmployeeTaskManagement.Infrastructure.Security;
using EmployeeTaskManagement.Infrastructure.UnitOfWorks;
using FluentValidation;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFileLogger(builder.Configuration, builder.Environment.ContentRootPath);

// 1. HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// 2. User Resolver Service
builder.Services.AddTransient<UserResolverService>();

// 3. Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.;Database=EmployeeTaskDb;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// 4. AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

// 5. FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(MappingProfile).Assembly);

// 6. Infrastructure & Security Services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.Configure<FileLoggingOptions>(builder.Configuration.GetSection("FileLogging"));
builder.Services.AddSingleton<ILogQueryService, LogQueryService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ITokenSessionService, TokenSessionService>();
builder.Services.AddSingleton<AuthCookieService>();

// 7. Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddHostedService<TokenCleanupService>();

// 8. Controllers & JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 9. JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Key"]
    ?? "TaskFlow_Ultra_Secure_Secret_Key_For_JWT_Authentication_2026_Enterprise_Grade!";

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? "EmployeeTaskManagementAPI";

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? "EmployeeTaskManagementClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token) &&
                context.Request.Cookies.TryGetValue(AuthCookieService.AccessCookie, out var cookieToken) &&
                !string.IsNullOrEmpty(cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId))
            {
                context.Fail("Invalid token subject.");
                return;
            }

            var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
            var scopeFactory = context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
            var isActive = await cache.GetOrCreateAsync(
                $"auth:active:{userId}",
                async ct =>
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    return await db.Users.AsNoTracking()
                        .Where(u => u.Id == userId && u.IsActive)
                        .AnyAsync(ct);
                },
                TimeSpan.FromSeconds(60),
                context.HttpContext.RequestAborted);

            if (!isActive)
            {
                context.Fail("Account is inactive or no longer exists.");
            }
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)),

        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,

        ValidateAudience = true,
        ValidAudience = jwtAudience,

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 10. CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
        if (origins == null || origins.Length == 0)
        {
            origins = new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000" };
        }
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("X-Correlation-ID");
    });
});

var authPermitLimit = int.TryParse(builder.Configuration["RateLimiting:AuthPermitLimit"], out var apl) && apl > 0 ? apl : 30;
var authWindowSeconds = int.TryParse(builder.Configuration["RateLimiting:AuthWindowSeconds"], out var aws) && aws > 0 ? aws : 60;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        var body = ApiResponse<object>.Fail("Too many requests. Please wait a moment and try again.", statusCode: 429);
        await ctx.HttpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(body, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }), ct);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromSeconds(authWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// 11. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Task Management API",
        Version = "v1",
        Description = "Enterprise RESTful Web API for Employee Task Management System with Clean Architecture, Dynamic Repository & Unit of Work."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token format: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// 12. Tracing, Security Headers & Global Exception Handling
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// 13. Database Migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogError(ex,
            "Database migration failed during application startup.");

        throw;
    }
}

// 14. Swagger
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Employee Task Management API v1");

    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.EnablePersistAuthorization();
    // Keep on a single line: Swashbuckle embeds this inside a JS single-quoted string passed to JSON.parse().
    c.UseRequestInterceptor(
        "function (req) { req.credentials = 'same-origin'; var method = (req.method || 'GET').toUpperCase(); var unsafe = method === 'POST' || method === 'PUT' || method === 'PATCH' || method === 'DELETE'; var hasAuth = Object.keys(req.headers || {}).some(function (k) { return k.toLowerCase() === 'authorization'; }); var url = (req.url || '').toLowerCase(); if (!unsafe || hasAuth || url.indexOf('/api/auth/login') !== -1 || req.headers['X-CSRF-TOKEN']) { return req; } return fetch('/api/auth/csrf', { credentials: 'same-origin' }).then(function (r) { return r.ok ? r.json() : null; }).then(function (j) { if (j && j.data) { req.headers['X-CSRF-TOKEN'] = j.data; } return req; }).catch(function () { return req; }); }");
});

// 15. Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseMiddleware<CsrfProtectionMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
