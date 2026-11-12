using System;

namespace EmployeeTaskManagement.Core.Entities
{
    public class TaskComment : IEntity
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation properties
        public virtual TaskEntity? Task { get; set; }
        public virtual User? User { get; set; }
    }
}
