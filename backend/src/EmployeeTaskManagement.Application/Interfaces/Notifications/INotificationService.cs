using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Notifications;

namespace EmployeeTaskManagement.Application.Interfaces.Notifications
{
    public class NotificationRequest
    {
        public IEnumerable<int> RecipientIds { get; set; } = new List<int>();
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public int? EntityId { get; set; }
        public string? Link { get; set; }
        public string? DedupeKey { get; set; }
    }

    public interface INotificationPublisher
    {
        Task PublishAsync(int userId, NotificationDto notification);
    }

    public interface INotificationService
    {
        /// <summary>Stages notifications in the current unit of work (persisted by the caller's SaveChanges).</summary>
        Task QueueAsync(NotificationRequest request);

        /// <summary>Pushes the staged notifications in real time after the business change has been committed.</summary>
        Task PublishQueuedAsync();

        Task<ApiResponse<PaginatedResult<NotificationDto>>> GetAsync(int userId, NotificationQueryParameters query);
        Task<ApiResponse<int>> GetUnreadCountAsync(int userId);
        Task<ApiResponse<bool>> MarkReadAsync(int userId, long id);
        Task<ApiResponse<int>> MarkAllReadAsync(int userId);
        Task<ApiResponse<IReadOnlyList<NotificationPreferenceDto>>> GetPreferencesAsync(int userId);
        Task<ApiResponse<bool>> SetPreferenceAsync(int userId, string category, bool enabled);
    }
}
