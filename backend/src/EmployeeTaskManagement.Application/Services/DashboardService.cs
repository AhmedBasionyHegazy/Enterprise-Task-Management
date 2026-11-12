using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Dashboard;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
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

        public DashboardService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync(int currentUserId, string currentUserRole)
        {
            var now = DateTime.UtcNow;

            // Fetch tasks based on role
            IEnumerable<TaskDto> allTasks;
            if (currentUserRole == RoleNames.Employee)
            {
                allTasks = await _unitOfWork.Tasks.GetAllTaskDtosAsync(t => t.AssignedToUserId == currentUserId);
            }
            else
            {
                allTasks = await _unitOfWork.Tasks.GetAllTaskDtosAsync();
            }

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
            var employees = await _unitOfWork.Users.GetEmployeesAsync();
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

            var summary = new DashboardSummaryDto
            {
                TotalTasks = totalTasks,
                NewTasks = newTasks,
                InProgressTasks = inProgressTasks,
                CompletedTasks = completedTasks,
                OverdueTasks = overdueTasks,
                TotalEmployees = employees.Count(),
                ActiveEmployees = employees.Count(e => e.IsActive),
                PriorityBreakdown = priorityBreakdown,
                StatusBreakdown = statusBreakdown,
                EmployeeWorkloads = employeeWorkloads,
                RecentTasks = recentTasks,
                OverdueTaskList = overdueTaskList
            };

            return ApiResponse<DashboardSummaryDto>.Ok(summary);
        }
    }
}
