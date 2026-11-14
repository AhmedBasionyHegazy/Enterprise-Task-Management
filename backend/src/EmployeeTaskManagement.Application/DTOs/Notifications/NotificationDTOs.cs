using System;

namespace EmployeeTaskManagement.Application.DTOs.Notifications
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public int? EntityId { get; set; }
        public string? Link { get; set; }
        public string? ActorName { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public class NotificationQueryParameters
    {
        public bool UnreadOnly { get; set; }
        public string? Category { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class NotificationPreferenceDto
    {
        public string Category { get; set; } = string.Empty;
        public bool InAppEnabled { get; set; } = true;
    }

    public class NotificationPreferenceUpdateDto
    {
        public bool InAppEnabled { get; set; }
    }
}
