using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Infrastructure.Repositories
{
    public class TaskCommentRepository : Repository<ApplicationDbContext, TaskComment, TaskCommentDto>, ITaskCommentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TaskCommentRepository(ApplicationDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TaskCommentDto>> GetCommentsByTaskIdAsync(int taskId)
        {
            var comments = await _context.TaskComments
                .Include(c => c.User)
                    .ThenInclude(u => u!.Role)
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TaskCommentDto>>(comments);
        }

        public void AddEntity(TaskComment comment)
        {
            _context.TaskComments.Add(comment);
        }
    }
}
