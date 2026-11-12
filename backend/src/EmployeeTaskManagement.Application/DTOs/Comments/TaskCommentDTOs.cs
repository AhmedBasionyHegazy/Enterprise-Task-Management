using System;

namespace EmployeeTaskManagement.Application.DTOs.Comments
{
    public class TaskCommentDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class TaskCommentCreateDto
    {
        public int TaskId { get; set; }
        public int? UserId { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
