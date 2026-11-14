using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeTaskManagement.Application.DTOs.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Notifications;

namespace EmployeeTaskManagement.API.Controllers
{
    /// <summary>All operations are bound to the authenticated user; ids from other users resolve to 404.</summary>
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] NotificationQueryParameters query) =>
            Ok(await _service.GetAsync(CurrentUserId, query));

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount() => Ok(await _service.GetUnreadCountAsync(CurrentUserId));

        [HttpPost("{id:long}/read")]
        public async Task<IActionResult> MarkRead(long id)
        {
            var result = await _service.MarkReadAsync(CurrentUserId, id);
            return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead() => Ok(await _service.MarkAllReadAsync(CurrentUserId));

        [HttpGet("preferences")]
        public async Task<IActionResult> Preferences() => Ok(await _service.GetPreferencesAsync(CurrentUserId));

        [HttpPut("preferences/{category}")]
        public async Task<IActionResult> SetPreference(string category, [FromBody] NotificationPreferenceUpdateDto body)
        {
            var result = await _service.SetPreferenceAsync(CurrentUserId, category, body.InAppEnabled);
            return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
        }
    }
}
