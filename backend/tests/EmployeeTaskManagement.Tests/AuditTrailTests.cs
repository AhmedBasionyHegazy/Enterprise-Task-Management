using AutoMapper;
using EmployeeTaskManagement.Application.DTOs.Audit;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using EmployeeTaskManagement.Application.Interfaces.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Mappings;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Audit;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using EmployeeTaskManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EmployeeTaskManagement.Tests
{
    public class AuditTrailTests
    {
        private static ApplicationDbContext CreateDbContext(string dbName, int userId = 1, string username = "admin")
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var ctx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username)
                }, "TestAuth"))
            };
            ctx.Items[UserResolverService.CorrelationItemKey] = "test-corr-id-001";

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(ctx);
            return new ApplicationDbContext(options, new UserResolverService(accessor.Object));
        }

        [Fact]
        public async Task DbContext_AutomaticallyCapturesCreateDiffs_UpdateDiffsOnly_AndSoftDeleteSnapshot()
        {
            var dbName = Guid.NewGuid().ToString("N");
            await using var db = CreateDbContext(dbName, 7, "sarah.admin");

            var task = new TaskEntity
            {
                Title = "Prepare Q4 Report",
                Description = "Initial draft",
                Priority = TaskPriorities.Medium,
                Status = TaskStatuses.New,
                StartDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(3),
                AssignedToUserId = 2,
                CreatedByUserId = 7
            };

            db.Tasks.Add(task);
            await db.SaveChangesAsync();

            var createAudit = await db.AuditLogs.SingleAsync(a => a.Action == AuditActions.Create);
            Assert.Equal("Task", createAudit.EntityName);
            Assert.Equal(task.Id.ToString(), createAudit.RecordId);
            Assert.Equal(task.Id, createAudit.RelatedTaskId);
            Assert.Equal(7, createAudit.UserId);
            Assert.Equal("test-corr-id-001", createAudit.CorrelationId);
            Assert.Contains("Prepare Q4 Report", createAudit.ChangesJson);

            // Modify ONLY Title and Priority (not Status/Description) -> unchanged fields must NOT appear
            task.Title = "Prepare Q4 Final Report";
            task.Priority = TaskPriorities.High;
            await db.SaveChangesAsync();

            var updateAudit = await db.AuditLogs.SingleAsync(a => a.Action == AuditActions.Update);
            var updateChanges = AuditChangeSerializer.Deserialize(updateAudit.ChangesJson);
            Assert.Equal(2, updateChanges.Count);
            Assert.Contains(updateChanges, c => c.Field == "Title" && c.Old == "Prepare Q4 Report" && c.New == "Prepare Q4 Final Report");
            Assert.Contains(updateChanges, c => c.Field == "Priority" && c.Old == "Medium" && c.New == "High");

            // Status change classifies action as StatusChange
            task.Status = TaskStatuses.InProgress;
            await db.SaveChangesAsync();
            var statusAudit = await db.AuditLogs.SingleAsync(a => a.Action == AuditActions.StatusChange);
            Assert.Contains("In Progress", statusAudit.ChangesJson);

            // Reassignment classifies action as Assign
            task.AssignedToUserId = 3;
            await db.SaveChangesAsync();
            var assignAudit = await db.AuditLogs.SingleAsync(a => a.Action == AuditActions.Assign);
            var assignChanges = AuditChangeSerializer.Deserialize(assignAudit.ChangesJson);
            Assert.Contains(assignChanges, c => c.Field == "AssignedToUserId" && c.Old == "2" && c.New == "3");

            // Soft delete preserves old snapshot and emits Delete action
            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
            var deleteAudit = await db.AuditLogs.SingleAsync(a => a.Action == AuditActions.Delete);
            Assert.Contains("Prepare Q4 Final Report", deleteAudit.ChangesJson);
        }

        [Fact]
        public async Task DbContext_MasksPasswordHash_AndEnforcesAuditImmutability()
        {
            await using var db = CreateDbContext(Guid.NewGuid().ToString("N"));
            db.Users.Add(new User
            {
                Username = "jdoe",
                Email = "jdoe@example.com",
                FullName = "John Doe",
                PasswordHash = "$2a$11$SuperSensitiveHashMaterial",
                RoleId = 2,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var audit = await db.AuditLogs.SingleAsync();
            Assert.DoesNotContain("SuperSensitiveHashMaterial", audit.ChangesJson);
            Assert.Contains("[hidden]", audit.ChangesJson);

            // Attempting to update or delete an AuditLog must throw
            audit.Description = "Tampered!";
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());

            db.ChangeTracker.Clear();
            db.AuditLogs.Remove(audit);
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task GetTaskHistoryAsync_BlocksEmployeeAccessingAnotherUsersTask_AndLogsAccessDenied()
        {
            var uow = new Mock<IUnitOfWork>();
            var taskRepo = new Mock<ITaskRepository>();
            var auditService = new Mock<IAuditService>();
            var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

            taskRepo.Setup(r => r.GetTaskEntityByIdAsync(42, false))
                .ReturnsAsync(new TaskEntity { Id = 42, Title = "Private", AssignedToUserId = 99 });
            uow.Setup(u => u.Tasks).Returns(taskRepo.Object);

            var service = new TaskService(
                uow.Object,
                mapper,
                auditService.Object,
                Mock.Of<INotificationService>(),
                Mock.Of<ICacheService>(),
                NullLogger<TaskService>.Instance);

            var result = await service.GetTaskHistoryAsync(42, new AuditQueryParameters(), currentUserId: 5, currentUserRole: RoleNames.Employee);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
            auditService.Verify(a => a.LogAsync(
                AuditActions.AccessDenied,
                "Task",
                "42",
                false,
                It.IsAny<string>(),
                null,
                null,
                42), Times.Once);
        }
    }
}
