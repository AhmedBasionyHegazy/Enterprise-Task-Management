using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Interfaces.Persistence
{
    public interface IRepository<TContext, T, TDto> 
        where T : class 
        where TContext : DbContext 
        where TDto : class
    {
        Task<TDto?> GetFirstOrDefault(Expression<Func<T, bool>> filter, bool? disableTracking = false, string? includeProperties = null);
        Task<IEnumerable<TDto>> GetAll(string? includeProperties = null);
        Task<IEnumerable<TDto>> GetAllRecords(Expression<Func<T, bool>>? filter = null, string? includeProperties = null);
        TDto Add(TDto entityDto);
        bool Remove(TDto entityDto);
        bool RemoveRange(IEnumerable<TDto> entityDtos);
        TDto Update(TDto entityDto);
        Task<int> SaveChanges();
    }

    public interface IUserRepository
    {
        Task<User?> GetUserEntityByIdAsync(int id, bool includeRole = true);
        Task<User?> GetUserEntityByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> IsUsernameTakenAsync(string username, int? excludeUserId = null);
        Task<bool> IsEmailTakenAsync(string email, int? excludeUserId = null);
        Task<IEnumerable<UserDto>> GetEmployeesAsync();
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        void UpdateEntity(User user);
        void AddEntity(User user);
    }

    public interface ITaskRepository
    {
        Task<PaginatedResult<TaskDto>> GetTasksPagedAsync(TaskQueryParameters query, int? restrictedUserId = null);
        Task<TaskEntity?> GetTaskEntityByIdAsync(int id, bool includeDetails = true);
        Task<TaskDto?> GetTaskDtoByIdAsync(int id);
        Task<IEnumerable<TaskDto>> GetAllTaskDtosAsync(Expression<Func<TaskEntity, bool>>? filter = null);
        void AddEntity(TaskEntity entity);
        void UpdateEntity(TaskEntity entity);
        void DeleteEntity(TaskEntity entity);
    }

    public interface ITaskCommentRepository
    {
        Task<IEnumerable<TaskCommentDto>> GetCommentsByTaskIdAsync(int taskId);
        void AddEntity(TaskComment comment);
    }

    public interface IRoleRepository
    {
        Task<Role?> GetRoleByNameAsync(string name);
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    }

    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        ITaskRepository Tasks { get; }
        ITaskCommentRepository TaskComments { get; }
        IRoleRepository Roles { get; }
        Task<int> SaveChanges();
        void StartTransaction();
        void CommitTransaction();
        void Rollback();
    }
}
