using System;
using System.Collections.Generic;

namespace EmployeeTaskManagement.Core.Entities
{
    public class TaskEntity : IEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty; // Low, Medium, High
        public string Status { get; set; } = string.Empty;   // New, In Progress, Completed
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public int AssignedToUserId { get; set; }
        public int CreatedByUserId { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation properties
        public virtual User? AssignedToUser { get; set; }
        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}
