using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using EmployeeTaskManagement.Application.DTOs.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Notifications;

namespace EmployeeTaskManagement.API.Hubs
{
    /// <summary>
    /// Server-to-client push only (no client-invokable methods). Each user is addressed through the
    /// NameIdentifier claim, so a connection can only ever receive its own owner's notifications.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly HashSet<string> _allowedOrigins;

        public NotificationHub(IConfiguration configuration)
        {
            _allowedOrigins = (configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public override Task OnConnectedAsync()
        {
            // Cross-site WebSocket hijacking guard.
            var origin = Context.GetHttpContext()?.Request.Headers.Origin.ToString();
            if (!string.IsNullOrEmpty(origin) && _allowedOrigins.Count > 0 && !_allowedOrigins.Contains(origin))
            {
                Context.Abort();
                return Task.CompletedTask;
            }
            return base.OnConnectedAsync();
        }
    }

    public class SignalRNotificationPublisher : INotificationPublisher
    {
        private readonly IHubContext<NotificationHub> _hub;

        public SignalRNotificationPublisher(IHubContext<NotificationHub> hub) => _hub = hub;

        public Task PublishAsync(int userId, NotificationDto notification) =>
            _hub.Clients.User(userId.ToString()).SendAsync("notification", notification);
    }
}
