using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;

namespace EmployeeTaskManagement.Infrastructure.Repositories
{
    /// <summary>Every read/update here is scoped by UserId: a user can never touch another user's notifications.</summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddRange(IEnumerable<Notification> notifications) => _context.Notifications.AddRange(notifications);

        public async Task<HashSet<int>> GetMutedUserIdsAsync(IEnumerable<int> userIds, string category)
        {
            var ids = userIds.ToList();
            var muted = await _context.NotificationPreferences.AsNoTracking()
                .Where(p => ids.Contains(p.UserId) && p.Category == category && !p.InAppEnabled)
                .Select(p => p.UserId).ToListAsync();
            return muted.ToHashSet();
        }

        public async Task<HashSet<int>> GetUsersWithDedupeKeyAsync(IEnumerable<int> userIds, string dedupeKey)
        {
            var ids = userIds.ToList();
            var existing = await _context.Notifications.AsNoTracking()
                .Where(n => ids.Contains(n.UserId) && n.DedupeKey == dedupeKey)
                .Select(n => n.UserId).ToListAsync();
            return existing.ToHashSet();
        }

        public async Task<PaginatedResult<NotificationDto>> GetForUserAsync(int userId, NotificationQueryParameters query)
        {
            var q = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (query.UnreadOnly) q = q.Where(n => !n.IsRead);
            if (!string.IsNullOrWhiteSpace(query.Category)) q = q.Where(n => n.Category == query.Category);

            var page = Math.Max(1, query.PageNumber);
            var size = Math.Clamp(query.PageSize, 1, 100);
            var total = await q.CountAsync();
            var rows = await q.OrderByDescending(n => n.CreatedUtc).ThenByDescending(n => n.Id)
                .Skip((page - 1) * size).Take(size).ToListAsync();

            return new PaginatedResult<NotificationDto>(
                rows.Select(n => new NotificationDto
                {
                    Id = n.Id, Type = n.Type, Category = n.Category, Title = n.Title, Message = n.Message,
                    EntityName = n.EntityName, EntityId = n.EntityId, Link = n.Link, ActorName = n.ActorName,
                    IsRead = n.IsRead, CreatedUtc = DateTime.SpecifyKind(n.CreatedUtc, DateTimeKind.Utc)
                }).ToList(), total, page, size);
        }

        public Task<int> GetUnreadCountAsync(int userId) =>
            _context.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task<bool> MarkReadAsync(int userId, long id)
        {
            var exists = await _context.Notifications.AnyAsync(n => n.Id == id && n.UserId == userId);
            if (!exists) return false;

            var now = DateTime.UtcNow;
            await _context.Notifications.Where(n => n.Id == id && n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadUtc, now));
            return true;
        }

        public Task<int> MarkAllReadAsync(int userId)
        {
            var now = DateTime.UtcNow;
            return _context.Notifications.Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadUtc, now));
        }

        public Task<List<NotificationPreferenceDto>> GetPreferencesAsync(int userId) =>
            _context.NotificationPreferences.AsNoTracking().Where(p => p.UserId == userId)
                .Select(p => new NotificationPreferenceDto { Category = p.Category, InAppEnabled = p.InAppEnabled })
                .ToListAsync();

        public async Task SetPreferenceAsync(int userId, string category, bool enabled)
        {
            var pref = await _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId && p.Category == category);
            if (pref == null)
                _context.NotificationPreferences.Add(new NotificationPreference { UserId = userId, Category = category, InAppEnabled = enabled });
            else
                pref.InAppEnabled = enabled;
        }
    }
}
