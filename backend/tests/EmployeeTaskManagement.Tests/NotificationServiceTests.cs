using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EmployeeTaskManagement.Tests
{
    public class NotificationServiceTests
    {
        private static UserResolverService ResolverFor(int userId, string username)
        {
            var ctx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username)
                }, "TestAuth"))
            };
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(ctx);
            return new UserResolverService(accessor.Object);
        }

        [Fact]
        public async Task QueueAndPublish_ExcludesActor_RespectsMutesAndDedupe_AndPublishesAfterCommit()
        {
            var uow = new Mock<IUnitOfWork>();
            var notifRepo = new Mock<INotificationRepository>();
            var publisher = new Mock<INotificationPublisher>();

            var persisted = new List<Notification>();
            notifRepo.Setup(r => r.GetMutedUserIdsAsync(It.IsAny<IEnumerable<int>>(), NotificationCategories.Task))
                .ReturnsAsync(new HashSet<int> { 3 }); // user 3 muted Task notifications
            notifRepo.Setup(r => r.GetUsersWithDedupeKeyAsync(It.IsAny<IEnumerable<int>>(), "dedupe-1"))
                .ReturnsAsync(new HashSet<int> { 4 }); // user 4 already notified
            notifRepo.Setup(r => r.AddRange(It.IsAny<IEnumerable<Notification>>()))
                .Callback<IEnumerable<Notification>>(items =>
                {
                    long id = 100;
                    foreach (var n in items)
                    {
                        n.Id = ++id;
                        persisted.Add(n);
                    }
                });

            uow.Setup(u => u.Notifications).Returns(notifRepo.Object);

            var service = new NotificationService(
                uow.Object,
                ResolverFor(1, "admin"),
                publisher.Object,
                NullLogger<NotificationService>.Instance);

            await service.QueueAsync(new NotificationRequest
            {
                RecipientIds = new[] { 1, 2, 3, 4, 2 }, // 1=actor (excluded), 3=muted, 4=dedupe, 2=valid (deduplicated once)
                Type = NotificationTypes.TaskAssigned,
                Category = NotificationCategories.Task,
                Title = "Assigned",
                Message = "Task assigned",
                EntityName = "Task",
                EntityId = 10,
                Link = "/tasks?open=10",
                DedupeKey = "dedupe-1"
            });

            Assert.Single(persisted);
            Assert.Equal(2, persisted[0].UserId);
            publisher.Verify(p => p.PublishAsync(It.IsAny<int>(), It.IsAny<NotificationDto>()), Times.Never);

            await service.PublishQueuedAsync();

            publisher.Verify(p => p.PublishAsync(2, It.Is<NotificationDto>(d => d.Id == 101 && d.Title == "Assigned")), Times.Once);
        }

        [Fact]
        public async Task MarkReadAsync_Returns404_WhenNotificationBelongsToAnotherUser()
        {
            var uow = new Mock<IUnitOfWork>();
            var notifRepo = new Mock<INotificationRepository>();
            notifRepo.Setup(r => r.MarkReadAsync(5, 999)).ReturnsAsync(false);
            uow.Setup(u => u.Notifications).Returns(notifRepo.Object);

            var service = new NotificationService(
                uow.Object,
                ResolverFor(5, "emp"),
                Mock.Of<INotificationPublisher>(),
                NullLogger<NotificationService>.Instance);

            var res = await service.MarkReadAsync(5, 999);
            Assert.False(res.Success);
            Assert.Equal(404, res.StatusCode);
        }
    }
}
