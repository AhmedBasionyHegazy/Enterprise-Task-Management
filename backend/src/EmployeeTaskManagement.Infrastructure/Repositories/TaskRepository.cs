using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Infrastructure.Repositories
{
    public class TaskRepository : Repository<ApplicationDbContext, TaskEntity, TaskDto>, ITaskRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public TaskRepository(ApplicationDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<TaskDto>> GetTasksPagedAsync(TaskQueryParameters query, int? restrictedUserId = null)
        {
            IQueryable<TaskEntity> tasksQuery = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.Comments)
                .AsNoTracking();

            // 1. Role / User restriction
            if (restrictedUserId.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.AssignedToUserId == restrictedUserId.Value);
            }
            else if (query.AssignedToUserId.HasValue && query.AssignedToUserId.Value > 0)
            {
                tasksQuery = tasksQuery.Where(t => t.AssignedToUserId == query.AssignedToUserId.Value);
            }

            // 2. Search filtering
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var trimmed = query.Search.Trim();
                var term = (trimmed.Length > 200 ? trimmed[..200] : trimmed).ToLower();
                tasksQuery = tasksQuery.Where(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term)));
            }

            // 3. Status filter
            if (!string.IsNullOrWhiteSpace(query.Status) && query.Status != "All")
            {
                tasksQuery = tasksQuery.Where(t => t.Status.ToLower() == query.Status.Trim().ToLower());
            }

            // 4. Priority filter
            if (!string.IsNullOrWhiteSpace(query.Priority) && query.Priority != "All")
            {
                tasksQuery = tasksQuery.Where(t => t.Priority.ToLower() == query.Priority.Trim().ToLower());
            }

            // 5. Total count before paging
            int totalCount = await tasksQuery.CountAsync();

            // 6. Dynamic server-side sorting
            tasksQuery = (query.SortBy?.ToLower(), query.SortDescending) switch
            {
                ("title", false) => tasksQuery.OrderBy(t => t.Title),
                ("title", true) => tasksQuery.OrderByDescending(t => t.Title),
                ("status", false) => tasksQuery.OrderBy(t => t.Status),
                ("status", true) => tasksQuery.OrderByDescending(t => t.Status),
                ("priority", false) => tasksQuery.OrderBy(t => t.Priority == "High" ? 1 : t.Priority == "Medium" ? 2 : 3),
                ("priority", true) => tasksQuery.OrderByDescending(t => t.Priority == "High" ? 1 : t.Priority == "Medium" ? 2 : 3),
                ("startdate", false) => tasksQuery.OrderBy(t => t.StartDate),
                ("startdate", true) => tasksQuery.OrderByDescending(t => t.StartDate),
                ("duedate", false) => tasksQuery.OrderBy(t => t.DueDate),
                ("duedate", true) => tasksQuery.OrderByDescending(t => t.DueDate),
                ("createddate", false) => tasksQuery.OrderBy(t => t.CreatedDate),
                ("createddate", true) => tasksQuery.OrderByDescending(t => t.CreatedDate),
                _ => tasksQuery.OrderBy(t => t.DueDate)
            };

            // 7. Pagination
            int pageNumber = Math.Max(1, query.PageNumber);
            int pageSize = query.PageSize > 0 ? Math.Min(query.PageSize, 100) : 10;
            var pagedEntities = await tasksQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<TaskDto>>(pagedEntities);
            return new PaginatedResult<TaskDto>(dtos, totalCount, pageNumber, pageSize);
        }

        public async Task<TaskEntity?> GetTaskEntityByIdAsync(int id, bool includeDetails = true)
        {
            IQueryable<TaskEntity> query = _context.Tasks;
            if (includeDetails)
            {
                query = query
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.User)
                            .ThenInclude(u => u!.Role);
            }
            return await query.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskDto?> GetTaskDtoByIdAsync(int id)
        {
            var entity = await GetTaskEntityByIdAsync(id, true);
            if (entity == null) return null;
            return _mapper.Map<TaskDto>(entity);
        }

        public async Task<IEnumerable<TaskDto>> GetAllTaskDtosAsync(Expression<Func<TaskEntity, bool>>? filter = null)
        {
            IQueryable<TaskEntity> query = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.Comments)
                .AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var entities = await query.ToListAsync();
            return _mapper.Map<IEnumerable<TaskDto>>(entities);
        }

        public void AddEntity(TaskEntity entity)
        {
            _context.Tasks.Add(entity);
        }

        public void UpdateEntity(TaskEntity entity)
        {
            _context.Tasks.Update(entity);
        }

        public void DeleteEntity(TaskEntity entity)
        {
            _context.Tasks.Remove(entity);
        }
    }
}
