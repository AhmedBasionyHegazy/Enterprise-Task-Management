using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;

namespace EmployeeTaskManagement.Infrastructure.Security
{
    public class AuditService : IAuditService
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly UserResolverService _resolver;
        private readonly ILogger<AuditService> _logger;

        public AuditService(DbContextOptions<ApplicationDbContext> options, UserResolverService resolver, ILogger<AuditService> logger)
        {
            _options = options;
            _resolver = resolver;
            _logger = logger;
        }

        public async Task LogAsync(string action, string entityName, string? recordId, bool isSuccess, string? description,
            int? userId = null, string? username = null, int? relatedTaskId = null)
        {
            try
            {
                // Dedicated context/connection: the event is persisted even if the caller's transaction rolls back.
                await using var db = new ApplicationDbContext(_options, _resolver);
                db.AuditLogs.Add(new AuditLog
                {
                    TimestampUtc = DateTime.UtcNow,
                    UserId = userId ?? _resolver.GetUserIdInt(),
                    Username = username ?? _resolver.GetUsername(),
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    RelatedTaskId = relatedTaskId,
                    IsSuccess = isSuccess,
                    Description = description is { Length: > 1000 } ? description[..1000] : description,
                    IpAddress = _resolver.GetIpAddress(),
                    UserAgent = _resolver.GetUserAgent(),
                    CorrelationId = _resolver.GetCorrelationId()
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Never silent: surfaced as an error log carrying the correlation id.
                _logger.LogError(ex, "AUDIT WRITE FAILED action={AuditAction} entity={AuditEntity} record={AuditRecord}",
                    action, entityName, recordId);
            }
        }
    }
}
