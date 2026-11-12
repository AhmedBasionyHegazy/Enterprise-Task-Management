using System;

namespace EmployeeTaskManagement.Application.DTOs.Tasks
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public int AssignedToUserId { get; set; }
        public string AssignedToUserName { get; set; } = string.Empty;
        public string AssignedToFullName { get; set; } = string.Empty;
        public string? AssignedToDepartment { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string CreatedByFullName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsOverdue => DueDate < DateTime.UtcNow && Status != "Completed";
        public int CommentsCount { get; set; }
    }

    public class TaskCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "New";
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public int AssignedToUserId { get; set; }
        public int? CreatedByUserId { get; set; }
    }

    public class TaskUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "New";
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public int AssignedToUserId { get; set; }
    }

    public class TaskStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class TaskQueryParameters
    {
        public string? Search { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? SortBy { get; set; } = "DueDate"; // DueDate, Priority, Status, CreatedDate, Title
        public bool SortDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
