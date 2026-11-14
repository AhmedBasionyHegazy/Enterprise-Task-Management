using EmployeeTaskManagement.Application.Interfaces.Security;

namespace EmployeeTaskManagement.API.BackgroundServices
{
    /// <summary>Scheduled job: purges expired / long-revoked refresh tokens. Fully traced with a job-run correlation id.</summary>
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly int _retentionDays;
        private readonly TimeSpan _interval;

        public TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _retentionDays = int.TryParse(configuration["Jwt:RevokedTokenRetentionDays"], out var d) && d > 0 ? d : 30;
            _interval = TimeSpan.FromHours(int.TryParse(configuration["Jwt:CleanupIntervalHours"], out var h) && h > 0 ? h : 6);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                var runId = Guid.NewGuid().ToString("N");
                using (_logger.BeginScope(new Dictionary<string, object> { ["Job"] = "RefreshTokenCleanup", ["CorrelationId"] = runId }))
                {
                    var started = DateTime.UtcNow;
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var sessions = scope.ServiceProvider.GetRequiredService<ITokenSessionService>();
                        var removed = await sessions.CleanupAsync(_retentionDays);
                        _logger.LogInformation("Background job finished. removed={Removed} durationMs={DurationMs}",
                            removed, (DateTime.UtcNow - started).TotalMilliseconds);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Background job failed.");
                    }
                }

                try { await Task.Delay(_interval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
