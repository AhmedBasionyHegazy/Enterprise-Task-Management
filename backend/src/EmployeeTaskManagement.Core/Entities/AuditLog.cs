using System;

namespace EmployeeTaskManagement.Core.Entities
{
    /// <summary>
    /// Append-only audit record. Intentionally NOT an IEntity (no soft delete / no modification).
    /// Immutability is enforced in code (DbContext guard) and in the database (INSTEAD OF trigger).
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public int? RelatedTaskId { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? Description { get; set; }
        public string? ChangesJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? CorrelationId { get; set; }
    }
}
