using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Audit;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Audit;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;

namespace EmployeeTaskManagement.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        // Fields holding user ids that are resolved to display names for readability.
        private static readonly HashSet<string> UserIdFields = new() { "AssignedToUserId", "CreatedByUserId", "UserId" };

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<PaginatedResult<AuditLogDto>> QueryAsync(AuditQueryParameters query, bool includeSensitive) =>
            RunAsync(_context.AuditLogs.AsNoTracking(), query, includeSensitive);

        public Task<PaginatedResult<AuditLogDto>> GetTaskHistoryAsync(int taskId, AuditQueryParameters query, bool includeSensitive) =>
            RunAsync(_context.AuditLogs.AsNoTracking()
                .Where(a => a.RelatedTaskId == taskId && (a.EntityName == "Task" || a.EntityName == "TaskComment")),
                query, includeSensitive);

        private async Task<PaginatedResult<AuditLogDto>> RunAsync(IQueryable<AuditLog> q, AuditQueryParameters p, bool includeSensitive)
        {
            if (p.UserId.HasValue) q = q.Where(a => a.UserId == p.UserId);
            if (!string.IsNullOrWhiteSpace(p.Username)) { var u = p.Username.Trim(); q = q.Where(a => a.Username != null && a.Username.Contains(u)); }
            if (!string.IsNullOrWhiteSpace(p.Action)) q = q.Where(a => a.Action == p.Action);
            if (!string.IsNullOrWhiteSpace(p.EntityName)) q = q.Where(a => a.EntityName == p.EntityName);
            if (!string.IsNullOrWhiteSpace(p.RecordId)) { var r = p.RecordId.Trim(); q = q.Where(a => a.RecordId == r); }
            if (p.Success.HasValue) q = q.Where(a => a.IsSuccess == p.Success);
            if (p.From.HasValue) { var f = p.From.Value.ToUniversalTime(); q = q.Where(a => a.TimestampUtc >= f); }
            if (p.To.HasValue) { var t = p.To.Value.ToUniversalTime(); q = q.Where(a => a.TimestampUtc <= t); }

            var page = Math.Max(1, p.PageNumber);
            var size = Math.Clamp(p.PageSize, 1, 100);

            var total = await q.CountAsync();
            var rows = await q
                .OrderByDescending(a => a.TimestampUtc).ThenByDescending(a => a.Id)
                .Skip((page - 1) * size).Take(size)
                .ToListAsync();

            var parsed = rows.Select(r => (Row: r, Changes: AuditChangeSerializer.Deserialize(r.ChangesJson))).ToList();

            // One query resolves every referenced user and actor (no N+1).
            var userIds = parsed.SelectMany(x => x.Changes)
                .Where(c => UserIdFields.Contains(c.Field))
                .SelectMany(c => new[] { c.Old, c.New })
                .Select(v => int.TryParse(v, out var id) ? id : (int?)null)
                .Where(id => id.HasValue).Select(id => id!.Value)
                .Concat(parsed.Where(x => x.Row.UserId.HasValue).Select(x => x.Row.UserId!.Value))
                .Distinct().ToList();

            var names = userIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Users.IgnoreQueryFilters().AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);

            string? Display(string field, string? value) =>
                UserIdFields.Contains(field) && int.TryParse(value, out var id) && names.TryGetValue(id, out var n) ? n : value;

            var items = parsed.Select(x => new AuditLogDto
            {
                Id = x.Row.Id,
                TimestampUtc = DateTime.SpecifyKind(x.Row.TimestampUtc, DateTimeKind.Utc),
                UserId = x.Row.UserId,
                Username = x.Row.Username,
                UserFullName = x.Row.UserId.HasValue && names.TryGetValue(x.Row.UserId.Value, out var full) ? full : x.Row.Username,
                Action = x.Row.Action,
                EntityName = x.Row.EntityName,
                RecordId = x.Row.RecordId,
                RelatedTaskId = x.Row.RelatedTaskId,
                IsSuccess = x.Row.IsSuccess,
                Description = x.Row.Description,
                Changes = x.Changes.Select(c => new AuditChangeDto
                {
                    Field = c.Field,
                    OldValue = c.Old,
                    NewValue = c.New,
                    OldDisplay = Display(c.Field, c.Old),
                    NewDisplay = Display(c.Field, c.New)
                }).ToList(),
                IpAddress = includeSensitive ? x.Row.IpAddress : null,
                UserAgent = includeSensitive ? x.Row.UserAgent : null,
                CorrelationId = includeSensitive ? x.Row.CorrelationId : null
            }).ToList();

            return new PaginatedResult<AuditLogDto>(items, total, page, size);
        }
    }
}
