using System;

namespace EmployeeTaskManagement.Core.Entities
{
    public class Notification
    {
        public long Id { get; set; }
        public int UserId { get; set; }          // recipient
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public int? EntityId { get; set; }
        public string? Link { get; set; }
        public int? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? DedupeKey { get; set; }
    }

    public class NotificationPreference
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool InAppEnabled { get; set; } = true;
    }
}
