using System;
using System.Collections.Generic;

namespace EmployeeTaskManagement.Application.DTOs.Audit
{
    public class AuditChangeDto
    {
        public string Field { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? OldDisplay { get; set; }
        public string? NewDisplay { get; set; }
    }

    public class AuditLogDto
    {
        public long Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserFullName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public int? RelatedTaskId { get; set; }
        public bool IsSuccess { get; set; }
        public string? Description { get; set; }
        public List<AuditChangeDto> Changes { get; set; } = new();
        // Only populated for administrators
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class AuditQueryParameters
    {
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? RecordId { get; set; }
        public bool? Success { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
