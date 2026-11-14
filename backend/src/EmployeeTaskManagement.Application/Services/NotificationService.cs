using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly UserResolverService _resolver;
        private readonly INotificationPublisher _publisher;
        private readonly ILogger<NotificationService> _logger;
        private readonly List<Notification> _pending = new();

        public NotificationService(
            IUnitOfWork uow,
            UserResolverService resolver,
            INotificationPublisher publisher,
            ILogger<NotificationService> logger)
        {
            _uow = uow;
            _resolver = resolver;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task QueueAsync(NotificationRequest request)
        {
            var actorId = _resolver.GetUserIdInt();

            // Never notify the actor about their own action; avoid duplicate recipients.
            var ids = (request.RecipientIds ?? Enumerable.Empty<int>())
                .Where(i => i > 0 && i != actorId)
                .Distinct()
                .ToList();
            if (ids.Count == 0) return;

            // Honour per-user preferences.
            var muted = await _uow.Notifications.GetMutedUserIdsAsync(ids, request.Category);
            if (muted != null) ids.RemoveAll(muted.Contains);

            // Prevent duplicates for the same event.
            if (!string.IsNullOrEmpty(request.DedupeKey) && ids.Count > 0)
            {
                var duplicates = await _uow.Notifications.GetUsersWithDedupeKeyAsync(ids, request.DedupeKey);
                if (duplicates != null) ids.RemoveAll(duplicates.Contains);
            }
            if (ids.Count == 0) return;

            var now = DateTime.UtcNow;
            var actorName = _resolver.GetUsername();
            var created = ids.Select(id => new Notification
            {
                UserId = id,
                Type = request.Type,
                Category = request.Category,
                Title = request.Title,
                Message = request.Message,
                EntityName = request.EntityName,
                EntityId = request.EntityId,
                Link = request.Link,
                ActorUserId = actorId,
                ActorName = actorName,
                CreatedUtc = now,
                DedupeKey = string.IsNullOrEmpty(request.DedupeKey) ? null : request.DedupeKey
            }).ToList();

            _uow.Notifications.AddRange(created);
            _pending.AddRange(created);
        }

        public async Task PublishQueuedAsync()
        {
            var toPublish = _pending.ToList();
            _pending.Clear();

            foreach (var n in toPublish)
            {
                if (n.Id == 0) continue; // not persisted
                try
                {
                    await _publisher.PublishAsync(n.UserId, ToDto(n));
                }
                catch (Exception ex)
                {
                    // Real-time push is best effort: the notification is already persisted.
                    _logger.LogWarning(ex, "Failed to push notification {NotificationId} to user {UserId}", n.Id, n.UserId);
                }
            }
        }

        public async Task<ApiResponse<PaginatedResult<NotificationDto>>> GetAsync(int userId, NotificationQueryParameters query)
        {
            var result = await _uow.Notifications.GetForUserAsync(userId, query);
            return ApiResponse<PaginatedResult<NotificationDto>>.Ok(result);
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync(int userId) =>
            ApiResponse<int>.Ok(await _uow.Notifications.GetUnreadCountAsync(userId));

        public async Task<ApiResponse<bool>> MarkReadAsync(int userId, long id)
        {
            var ok = await _uow.Notifications.MarkReadAsync(userId, id);
            return ok
                ? ApiResponse<bool>.Ok(true, "Notification marked as read.")
                : ApiResponse<bool>.Fail("Notification not found.", statusCode: 404);
        }

        public async Task<ApiResponse<int>> MarkAllReadAsync(int userId) =>
            ApiResponse<int>.Ok(await _uow.Notifications.MarkAllReadAsync(userId), "All notifications marked as read.");

        public async Task<ApiResponse<IReadOnlyList<NotificationPreferenceDto>>> GetPreferencesAsync(int userId)
        {
            var existing = await _uow.Notifications.GetPreferencesAsync(userId);
            var list = NotificationCategories.All
                .Select(c => existing.FirstOrDefault(p => p.Category == c) ?? new NotificationPreferenceDto { Category = c, InAppEnabled = true })
                .ToList();
            return ApiResponse<IReadOnlyList<NotificationPreferenceDto>>.Ok(list);
        }

        public async Task<ApiResponse<bool>> SetPreferenceAsync(int userId, string category, bool enabled)
        {
            if (!NotificationCategories.All.Contains(category))
                return ApiResponse<bool>.Fail("Unknown notification category.", statusCode: 400);

            await _uow.Notifications.SetPreferenceAsync(userId, category, enabled);
            await _uow.SaveChanges();
            return ApiResponse<bool>.Ok(enabled, "Preference saved.");
        }

        public static NotificationDto ToDto(Notification n) => new()
        {
            Id = n.Id,
            Type = n.Type,
            Category = n.Category,
            Title = n.Title,
            Message = n.Message,
            EntityName = n.EntityName,
            EntityId = n.EntityId,
            Link = n.Link,
            ActorName = n.ActorName,
            IsRead = n.IsRead,
            CreatedUtc = n.CreatedUtc
        };
    }
}
