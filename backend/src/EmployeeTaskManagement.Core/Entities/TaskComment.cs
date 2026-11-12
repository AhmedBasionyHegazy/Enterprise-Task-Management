using System;

namespace EmployeeTaskManagement.Core.Entities
{
    public class TaskComment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual TaskEntity? Task { get; set; }
        public virtual User? User { get; set; }
    }
}
