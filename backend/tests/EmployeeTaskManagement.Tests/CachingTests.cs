using AutoMapper;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Mappings;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using EmployeeTaskManagement.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace EmployeeTaskManagement.Tests
{
    public class CachingTests
    {
        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        [Fact]
        public async Task MemoryCacheService_PreventsStampedeUnderConcurrency()
        {
            using var memory = new MemoryCache(new MemoryCacheOptions());
            var cache = new MemoryCacheService(memory);
            var invocations = 0;

            var tasks = Enumerable.Range(0, 12).Select(_ =>
                cache.GetOrCreateAsync("heavy:key", async ct =>
                {
                    Interlocked.Increment(ref invocations);
                    await Task.Delay(35, ct);
                    return "computed";
                }, TimeSpan.FromMinutes(1)));

            var results = await Task.WhenAll(tasks);

            Assert.All(results, r => Assert.Equal("computed", r));
            Assert.Equal(1, invocations);
        }

        [Fact]
        public async Task DashboardService_CachesPerRoleAndUser_AndInvalidatesOnVersionBump()
        {
            using var memory = new MemoryCache(new MemoryCacheOptions());
            var cache = new MemoryCacheService(memory);
            var uow = new Mock<IUnitOfWork>();
            var taskRepo = new Mock<ITaskRepository>();
            var userRepo = new Mock<IUserRepository>();

            var adminCalls = 0;
            taskRepo.Setup(r => r.GetAllTaskDtosAsync(null))
                .ReturnsAsync(() =>
                {
                    adminCalls++;
                    return new List<TaskDto>
                    {
                        new() { Id = 1, Title = "T1", Status = TaskStatuses.New, Priority = TaskPriorities.High, DueDate = DateTime.UtcNow.AddDays(2), AssignedToUserId = 2 },
                        new() { Id = 2, Title = "T2", Status = TaskStatuses.Completed, Priority = TaskPriorities.Low, DueDate = DateTime.UtcNow.AddDays(1), AssignedToUserId = 3 }
                    };
                });

            taskRepo.Setup(r => r.GetAllTaskDtosAsync(It.IsNotNull<Expression<Func<TaskEntity, bool>>>()))
                .ReturnsAsync(new List<TaskDto>
                {
                    new() { Id = 1, Title = "T1", Status = TaskStatuses.New, Priority = TaskPriorities.High, DueDate = DateTime.UtcNow.AddDays(2), AssignedToUserId = 2 }
                });

            userRepo.Setup(r => r.GetEmployeesAsync())
                .ReturnsAsync(new List<UserDto> { new() { Id = 2, FullName = "Emp 1", IsActive = true } });

            uow.Setup(u => u.Tasks).Returns(taskRepo.Object);
            uow.Setup(u => u.Users).Returns(userRepo.Object);

            var service = new DashboardService(uow.Object, CreateMapper(), cache);

            var a1 = await service.GetDashboardSummaryAsync(1, RoleNames.Admin);
            var a2 = await service.GetDashboardSummaryAsync(1, RoleNames.Admin);
            var e1 = await service.GetDashboardSummaryAsync(2, RoleNames.Employee);

            Assert.Equal(2, a1.Data!.TotalTasks);
            Assert.Equal(2, a2.Data!.TotalTasks);
            Assert.Equal(1, e1.Data!.TotalTasks);
            Assert.Equal(1, adminCalls); // second Admin read hit cache, Employee read did NOT see Admin cache

            await cache.BumpVersionAsync(CacheScopes.Dashboard);
            await service.GetDashboardSummaryAsync(1, RoleNames.Admin);
            Assert.Equal(2, adminCalls); // cache cleanly busted after version bump
        }
    }
}
