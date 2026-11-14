using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using EmployeeTaskManagement.Application.Interfaces.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using EmployeeTaskManagement.Application.Mappings;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EmployeeTaskManagement.Tests
{
    public class TaskServiceTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            var provider = services.BuildServiceProvider();
            _mapper = provider.GetRequiredService<IMapper>();

            _mockUow = new Mock<IUnitOfWork>();
            _taskService = new TaskService(
                _mockUow.Object,
                _mapper,
                Mock.Of<IAuditService>(),
                Mock.Of<INotificationService>(),
                Mock.Of<ICacheService>(),
                NullLogger<TaskService>.Instance);
        }

        [Fact]
        public async Task GetTasksAsync_WhenEmployee_RestrictsQueryToAssignedUserId()
        {
            // Arrange
            var query = new TaskQueryParameters { PageNumber = 1, PageSize = 10 };
            int employeeUserId = 2;
            string role = RoleNames.Employee;

            var fakeTasks = new List<TaskDto>
            {
                new TaskDto { Id = 1, Title = "Test Task 1", AssignedToUserId = employeeUserId, Status = "New" }
            };
            var pagedResult = new PaginatedResult<TaskDto>(fakeTasks, 1, 1, 10);

            _mockUow.Setup(u => u.Tasks.GetTasksPagedAsync(query, employeeUserId))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _taskService.GetTasksAsync(query, employeeUserId, role);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data.Items);
            _mockUow.Verify(u => u.Tasks.GetTasksPagedAsync(query, employeeUserId), Times.Once);
        }

        [Fact]
        public async Task UpdateTaskStatusAsync_WhenEmployeeAttemptsToModifyAnotherEmployeesTask_ReturnsForbidden()
        {
            // Arrange
            int taskId = 10;
            int currentUserId = 2;
            string role = RoleNames.Employee;

            var existingTask = new TaskEntity
            {
                Id = taskId,
                Title = "Another Employee's Task",
                AssignedToUserId = 99, // Assigned to user 99, not user 2!
                Status = "New"
            };

            _mockUow.Setup(u => u.Tasks.GetTaskEntityByIdAsync(taskId, false))
                .ReturnsAsync(existingTask);

            // Act
            var result = await _taskService.UpdateTaskStatusAsync(taskId, "In Progress", currentUserId, role);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task UpdateTaskStatusAsync_WhenValid_UpdatesStatusSuccessfully()
        {
            // Arrange
            int taskId = 10;
            int currentUserId = 2;
            string role = RoleNames.Employee;

            var existingTask = new TaskEntity
            {
                Id = taskId,
                Title = "My Task",
                AssignedToUserId = currentUserId,
                Status = "New"
            };

            var updatedDto = new TaskDto
            {
                Id = taskId,
                Title = "My Task",
                AssignedToUserId = currentUserId,
                Status = "Completed"
            };

            _mockUow.Setup(u => u.Tasks.GetTaskEntityByIdAsync(taskId, false))
                .ReturnsAsync(existingTask);
            _mockUow.Setup(u => u.Tasks.GetTaskDtoByIdAsync(taskId))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _taskService.UpdateTaskStatusAsync(taskId, "Completed", currentUserId, role);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Completed", result.Data!.Status);
            _mockUow.Verify(u => u.Tasks.UpdateEntity(It.Is<TaskEntity>(t => t.Status == "Completed")), Times.Once);
            _mockUow.Verify(u => u.SaveChanges(), Times.Once);
        }
    }
}
