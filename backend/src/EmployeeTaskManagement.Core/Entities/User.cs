using System;
using System.Collections.Generic;

namespace EmployeeTaskManagement.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual Role? Role { get; set; }
        public virtual ICollection<TaskEntity> AssignedTasks { get; set; } = new List<TaskEntity>();
        public virtual ICollection<TaskEntity> CreatedTasks { get; set; } = new List<TaskEntity>();
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}
