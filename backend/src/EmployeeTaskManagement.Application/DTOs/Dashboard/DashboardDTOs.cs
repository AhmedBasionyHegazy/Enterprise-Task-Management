using System.Collections.Generic;
using EmployeeTaskManagement.Application.DTOs.Tasks;

namespace EmployeeTaskManagement.Application.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int TotalTasks { get; set; }
        public int NewTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }

        public List<MetricCountDto> PriorityBreakdown { get; set; } = new List<MetricCountDto>();
        public List<MetricCountDto> StatusBreakdown { get; set; } = new List<MetricCountDto>();
        public List<EmployeeWorkloadDto> EmployeeWorkloads { get; set; } = new List<EmployeeWorkloadDto>();
        public List<TaskDto> RecentTasks { get; set; } = new List<TaskDto>();
        public List<TaskDto> OverdueTaskList { get; set; } = new List<TaskDto>();
    }

    public class MetricCountDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class EmployeeWorkloadDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int AssignedTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public int PendingTasksCount { get; set; }
        public int OverdueTasksCount { get; set; }
    }
}
