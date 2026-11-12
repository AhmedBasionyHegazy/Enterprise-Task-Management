using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Services
{
    public interface ITaskService
    {
        Task<ApiResponse<PaginatedResult<TaskDto>>> GetTasksAsync(TaskQueryParameters query, int currentUserId, string currentUserRole);
        Task<ApiResponse<TaskDto>> GetTaskByIdAsync(int id, int currentUserId, string currentUserRole);
        Task<ApiResponse<TaskDto>> CreateTaskAsync(TaskCreateDto request, int currentUserId);
        Task<ApiResponse<TaskDto>> UpdateTaskAsync(TaskUpdateDto request, int currentUserId);
        Task<ApiResponse<bool>> DeleteTaskAsync(int id, int currentUserId);
        Task<ApiResponse<TaskDto>> UpdateTaskStatusAsync(int id, string newStatus, int currentUserId, string currentUserRole);
        Task<ApiResponse<IEnumerable<TaskCommentDto>>> GetTaskCommentsAsync(int taskId, int currentUserId, string currentUserRole);
        Task<ApiResponse<TaskCommentDto>> AddCommentAsync(TaskCommentCreateDto request, int currentUserId, string currentUserRole);
    }

    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TaskService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PaginatedResult<TaskDto>>> GetTasksAsync(TaskQueryParameters query, int currentUserId, string currentUserRole)
        {
            int? restrictedUserId = null;
            if (currentUserRole == RoleNames.Employee)
            {
                // Employees can only view tasks assigned to them
                restrictedUserId = currentUserId;
            }

            var result = await _unitOfWork.Tasks.GetTasksPagedAsync(query, restrictedUserId);
            return ApiResponse<PaginatedResult<TaskDto>>.Ok(result);
        }

        public async Task<ApiResponse<TaskDto>> GetTaskByIdAsync(int id, int currentUserId, string currentUserRole)
        {
            var task = await _unitOfWork.Tasks.GetTaskEntityByIdAsync(id, includeDetails: true);
            if (task == null)
            {
                return ApiResponse<TaskDto>.Fail("Task not found.", statusCode: 404);
            }

            if (currentUserRole == RoleNames.Employee && task.AssignedToUserId != currentUserId)
            {
                return ApiResponse<TaskDto>.Fail("You are not authorized to view this task.", statusCode: 403);
            }

            var dto = _mapper.Map<TaskDto>(task);
            return ApiResponse<TaskDto>.Ok(dto);
        }

        public async Task<ApiResponse<TaskDto>> CreateTaskAsync(TaskCreateDto request, int currentUserId)
        {
            // Verify assigned user exists and is active
            var assignedUser = await _unitOfWork.Users.GetUserEntityByIdAsync(request.AssignedToUserId, includeRole: false);
            if (assignedUser == null || !assignedUser.IsActive)
            {
                return ApiResponse<TaskDto>.Fail("Assigned employee not found or is currently inactive.", statusCode: 400);
            }

            _unitOfWork.StartTransaction();
            try
            {
                var task = _mapper.Map<TaskEntity>(request);
                task.CreatedByUserId = currentUserId;
                task.CreatedDate = DateTime.UtcNow;

                _unitOfWork.Tasks.AddEntity(task);
                await _unitOfWork.SaveChanges();

                _unitOfWork.CommitTransaction();

                var createdDto = await _unitOfWork.Tasks.GetTaskDtoByIdAsync(task.Id);
                return ApiResponse<TaskDto>.Ok(createdDto!, "Task created and assigned successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return ApiResponse<TaskDto>.Fail($"Error creating task: {ex.Message}", statusCode: 500);
            }
        }

        public async Task<ApiResponse<TaskDto>> UpdateTaskAsync(TaskUpdateDto request, int currentUserId)
        {
            var task = await _unitOfWork.Tasks.GetTaskEntityByIdAsync(request.Id, includeDetails: false);
            if (task == null)
            {
                return ApiResponse<TaskDto>.Fail("Task not found.", statusCode: 404);
            }

            var assignedUser = await _unitOfWork.Users.GetUserEntityByIdAsync(request.AssignedToUserId, includeRole: false);
            if (assignedUser == null || !assignedUser.IsActive)
            {
                return ApiResponse<TaskDto>.Fail("Assigned employee not found or is currently inactive.", statusCode: 400);
            }

            _unitOfWork.StartTransaction();
            try
            {
                task.Title = request.Title.Trim();
                task.Description = request.Description?.Trim();
                task.Priority = request.Priority;
                task.Status = request.Status;
                task.StartDate = request.StartDate;
                task.DueDate = request.DueDate;
                task.AssignedToUserId = request.AssignedToUserId;
                task.ModifiedDate = DateTime.UtcNow;

                _unitOfWork.Tasks.UpdateEntity(task);
                await _unitOfWork.SaveChanges();
                _unitOfWork.CommitTransaction();

                var updatedDto = await _unitOfWork.Tasks.GetTaskDtoByIdAsync(task.Id);
                return ApiResponse<TaskDto>.Ok(updatedDto!, "Task updated successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return ApiResponse<TaskDto>.Fail($"Error updating task: {ex.Message}", statusCode: 500);
            }
        }

        public async Task<ApiResponse<bool>> DeleteTaskAsync(int id, int currentUserId)
        {
            var task = await _unitOfWork.Tasks.GetTaskEntityByIdAsync(id, includeDetails: false);
            if (task == null)
            {
                return ApiResponse<bool>.Fail("Task not found.", statusCode: 404);
            }

            _unitOfWork.StartTransaction();
            try
            {
                _unitOfWork.Tasks.DeleteEntity(task);
                await _unitOfWork.SaveChanges();
                _unitOfWork.CommitTransaction();

                return ApiResponse<bool>.Ok(true, "Task deleted successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return ApiResponse<bool>.Fail($"Error deleting task: {ex.Message}", statusCode: 500);
            }
        }

        public async Task<ApiResponse<TaskDto>> UpdateTaskStatusAsync(int id, string newStatus, int currentUserId, string currentUserRole)
        {
            if (!TaskStatuses.All.Contains(newStatus))
            {
                return ApiResponse<TaskDto>.Fail("Invalid status value. Must be 'New', 'In Progress', or 'Completed'.", statusCode: 400);
            }

            var task = await _unitOfWork.Tasks.GetTaskEntityByIdAsync(id, includeDetails: false);
            if (task == null)
            {
                return ApiResponse<TaskDto>.Fail("Task not found.", statusCode: 404);
            }

            if (currentUserRole == RoleNames.Employee && task.AssignedToUserId != currentUserId)
            {
                return ApiResponse<TaskDto>.Fail("You can only update the status of tasks assigned to you.", statusCode: 403);
            }

            task.Status = newStatus;
            task.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Tasks.UpdateEntity(task);
            await _unitOfWork.SaveChanges();

            var updatedDto = await _unitOfWork.Tasks.GetTaskDtoByIdAsync(task.Id);
            return ApiResponse<TaskDto>.Ok(updatedDto!, $"Task status updated to '{newStatus}'.");
        }

        public async Task<ApiResponse<IEnumerable<TaskCommentDto>>> GetTaskCommentsAsync(int taskId, int currentUserId, string currentUserRole)
        {
            var task = await _unitOfWork.Tasks.GetTaskEntityByIdAsync(taskId, includeDetails: false);
            if (task == null)
            {
                return ApiResponse<IEnumerable<TaskCommentDto>>.Fail("Task not found.", statusCode: 404);
            }

            if (currentUserRole == RoleNames.Employee && task.AssignedToUserId != currentUserId)
            {
                return ApiResponse<IEnumerable<TaskCommentDto>>.Fail("You are not authorized to view comments for this task.", statusCode: 403);
            }

            var comments = await _unitOfWork.TaskComments.GetCommentsByTaskIdAsync(taskId);
            return ApiResponse<IEnumerable<TaskCommentDto>>.Ok(comments);
        }

        public async Task<ApiResponse<TaskCommentDto>> AddCommentAsync(TaskCommentCreateDto request, int currentUserId, string currentUserRole)
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                return ApiResponse<TaskCommentDto>.Fail("Comment cannot be empty.", statusCode: 400);
            }

            var task = await _unitOfWork.Tasks.GetTaskEntityByIdAsync(request.TaskId, includeDetails: false);
            if (task == null)
            {
                return ApiResponse<TaskCommentDto>.Fail("Task not found.", statusCode: 404);
            }

            if (currentUserRole == RoleNames.Employee && task.AssignedToUserId != currentUserId)
            {
                return ApiResponse<TaskCommentDto>.Fail("You can only add comments to tasks assigned to you.", statusCode: 403);
            }

            var comment = new TaskComment
            {
                TaskId = request.TaskId,
                UserId = currentUserId,
                Comment = request.Comment.Trim(),
                CreatedDate = DateTime.UtcNow
            };

            _unitOfWork.TaskComments.AddEntity(comment);
            await _unitOfWork.SaveChanges();

            var user = await _unitOfWork.Users.GetUserEntityByIdAsync(currentUserId, includeRole: true);

            var commentDto = new TaskCommentDto
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                UserId = currentUserId,
                UserName = user?.Username ?? string.Empty,
                UserFullName = user?.FullName ?? string.Empty,
                UserRole = user?.Role?.Name ?? string.Empty,
                Comment = comment.Comment,
                CreatedDate = comment.CreatedDate
            };

            return ApiResponse<TaskCommentDto>.Ok(commentDto, "Comment added successfully.");
        }
    }
}
