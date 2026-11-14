using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Dashboard;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Services
{
    public interface IDashboardService
    {
        Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync(int currentUserId, string currentUserRole);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;

        public DashboardService(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync(int currentUserId, string currentUserRole)
        {
            var dashVersion = await _cache.GetVersionAsync(CacheScopes.Dashboard);
            var usersVersion = await _cache.GetVersionAsync(CacheScopes.Users);
            // Keyed per role and user id so an employee can never read cached admin metrics.
            var key = $"dashboard:v{dashVersion}:u{usersVersion}:{currentUserRole}:{currentUserId}";

            var summary = await _cache.GetOrCreateAsync(
                key,
                _ => BuildSummaryAsync(currentUserId, currentUserRole),
                TimeSpan.FromSeconds(60));

            return ApiResponse<DashboardSummaryDto>.Ok(summary);
        }

        private async Task<DashboardSummaryDto> BuildSummaryAsync(int currentUserId, string currentUserRole)
        {
            var now = DateTime.UtcNow;

            // Fetch tasks based on role
            IEnumerable<TaskDto> allTasks = currentUserRole == RoleNames.Employee
                ? await _unitOfWork.Tasks.GetAllTaskDtosAsync(t => t.AssignedToUserId == currentUserId)
                : await _unitOfWork.Tasks.GetAllTaskDtosAsync();

            var taskList = allTasks.ToList();

            var totalTasks = taskList.Count;
            var newTasks = taskList.Count(t => t.Status == TaskStatuses.New);
            var inProgressTasks = taskList.Count(t => t.Status == TaskStatuses.InProgress);
            var completedTasks = taskList.Count(t => t.Status == TaskStatuses.Completed);
            var overdueTasks = taskList.Count(t => t.DueDate < now && t.Status != TaskStatuses.Completed);

            // Priority breakdown
            var priorities = new[] { TaskPriorities.High, TaskPriorities.Medium, TaskPriorities.Low };
            var priorityBreakdown = priorities.Select(p =>
            {
                var count = taskList.Count(t => t.Priority == p);
                return new MetricCountDto
                {
                    Label = p,
                    Count = count,
                    Percentage = totalTasks > 0 ? Math.Round((double)count / totalTasks * 100, 1) : 0
                };
            }).ToList();

            // Status breakdown
            var statuses = new[] { TaskStatuses.New, TaskStatuses.InProgress, TaskStatuses.Completed };
            var statusBreakdown = statuses.Select(s =>
            {
                var count = taskList.Count(t => t.Status == s);
                return new MetricCountDto
                {
                    Label = s,
                    Count = count,
                    Percentage = totalTasks > 0 ? Math.Round((double)count / totalTasks * 100, 1) : 0
                };
            }).ToList();

            // Employee Workloads
            var employees = (await _unitOfWork.Users.GetEmployeesAsync()).ToList();
            var employeeWorkloads = new List<EmployeeWorkloadDto>();

            if (currentUserRole == RoleNames.Admin)
            {
                foreach (var emp in employees)
                {
                    var empTasks = taskList.Where(t => t.AssignedToUserId == emp.Id).ToList();
                    employeeWorkloads.Add(new EmployeeWorkloadDto
                    {
                        UserId = emp.Id,
                        FullName = emp.FullName,
                        Department = emp.Department ?? "Engineering",
                        AssignedTasksCount = empTasks.Count,
                        CompletedTasksCount = empTasks.Count(t => t.Status == TaskStatuses.Completed),
                        PendingTasksCount = empTasks.Count(t => t.Status != TaskStatuses.Completed),
                        OverdueTasksCount = empTasks.Count(t => t.DueDate < now && t.Status != TaskStatuses.Completed)
                    });
                }
            }

            // Recent tasks (top 5 by created date desc)
            var recentTasks = taskList
                .OrderByDescending(t => t.CreatedDate)
                .Take(5)
                .ToList();

            // Overdue tasks list
            var overdueTaskList = taskList
                .Where(t => t.DueDate < now && t.Status != TaskStatuses.Completed)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToList();

            return new DashboardSummaryDto
            {
                TotalTasks = totalTasks,
                NewTasks = newTasks,
                InProgressTasks = inProgressTasks,
                CompletedTasks = completedTasks,
                OverdueTasks = overdueTasks,
                TotalEmployees = employees.Count,
                ActiveEmployees = employees.Count(e => e.IsActive),
                PriorityBreakdown = priorityBreakdown,
                StatusBreakdown = statusBreakdown,
                EmployeeWorkloads = employeeWorkloads,
                RecentTasks = recentTasks,
                OverdueTaskList = overdueTaskList
            };
        }
    }
}
