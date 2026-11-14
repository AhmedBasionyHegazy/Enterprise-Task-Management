using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Audit;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.Interfaces.Audit;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using EmployeeTaskManagement.Application.Interfaces.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Task<ApiResponse<PaginatedResult<AuditLogDto>>> GetTaskHistoryAsync(int taskId, AuditQueryParameters query, int currentUserId, string currentUserRole);
    }

    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly ICacheService _cache;
        private readonly ILogger<TaskService> _logger;

        public TaskService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAuditService audit,
            INotificationService notifications,
            ICacheService cache,
            ILogger<TaskService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _audit = audit;
            _notifications = notifications;
            _cache = cache;
            _logger = logger;
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
                await Denied(id, "Attempted to view a task that is not assigned to the user.");
                return ApiResponse<TaskDto>.Fail("You are not authorized to view this task.", statusCode: 403);
            }

            return ApiResponse<TaskDto>.Ok(_mapper.Map<TaskDto>(task));
        }

        public async Task<ApiResponse<TaskDto>> CreateTaskAsync(TaskCreateDto request, int currentUserId)
        {
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

                await _notifications.QueueAsync(new NotificationRequest
                {
                    RecipientIds = new[] { task.AssignedToUserId },
                    Type = NotificationTypes.TaskAssigned,
                    Category = NotificationCategories.Task,
                    Title = "New task assigned to you",
                    Message = $"\"{task.Title}\" was created and assigned to you. Due {task.DueDate:MMM d, yyyy}.",
                    EntityName = "Task",
                    EntityId = task.Id,
                    Link = TaskLink(task.Id),
                    DedupeKey = Key("created", task.Id)
                });
                await _unitOfWork.SaveChanges();

                _unitOfWork.CommitTransaction();
                await _cache.BumpVersionAsync(CacheScopes.Dashboard);
                await _notifications.PublishQueuedAsync();

                _logger.LogInformation("Task action={Action} taskId={TaskId} userId={UserId} assignedTo={AssignedToUserId}",
                    AuditActions.Create, task.Id, currentUserId, task.AssignedToUserId);

                var createdDto = await _unitOfWork.Tasks.GetTaskDtoByIdAsync(task.Id);
                return ApiResponse<TaskDto>.Ok(createdDto!, "Task created and assigned successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                _logger.LogError(ex, "Error creating task action={Action} userId={UserId}", AuditActions.Create, currentUserId);
                return ApiResponse<TaskDto>.Fail("An error occurred while creating the task.", statusCode: 500);
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

            var oldAssignee = task.AssignedToUserId;
            var oldStatus = task.Status;
            var title = request.Title.Trim();
            var description = request.Description?.Trim();
            var otherFieldsChanged =
                task.Title != title || task.Description != description || task.Priority != request.Priority ||
                task.StartDate != request.StartDate || task.DueDate != request.DueDate;
            var assigneeChanged = oldAssignee != request.AssignedToUserId;
            var statusChanged = oldStatus != request.Status;

            _unitOfWork.StartTransaction();
            try
            {
                task.Title = title;
                task.Description = description;
                task.Priority = request.Priority;
                task.Status = request.Status;
                task.StartDate = request.StartDate;
                task.DueDate = request.DueDate;
                task.AssignedToUserId = request.AssignedToUserId;
                task.ModifiedDate = DateTime.UtcNow;

                if (assigneeChanged)
                {
                    await _notifications.QueueAsync(new NotificationRequest
                    {
                        RecipientIds = new[] { request.AssignedToUserId },
                        Type = NotificationTypes.TaskReassigned,
                        Category = NotificationCategories.Task,
                        Title = "Task assigned to you",
                        Message = $"\"{task.Title}\" has been assigned to you. Due {task.DueDate:MMM d, yyyy}.",
                        EntityName = "Task", EntityId = task.Id, Link = TaskLink(task.Id),
                        DedupeKey = Key("assigned", task.Id)
                    });
                    await _notifications.QueueAsync(new NotificationRequest
                    {
                        RecipientIds = new[] { oldAssignee },
                        Type = NotificationTypes.TaskUnassigned,
                        Category = NotificationCategories.Task,
                        Title = "Task reassigned",
                        Message = $"\"{task.Title}\" is no longer assigned to you.",
                        EntityName = "Task", EntityId = task.Id, Link = null,
                        DedupeKey = Key("unassigned", task.Id)
                    });
                }
                else if (otherFieldsChanged || statusChanged)
                {
                    await _notifications.QueueAsync(new NotificationRequest
                    {
                        RecipientIds = new[] { task.AssignedToUserId },
                        Type = statusChanged ? NotificationTypes.TaskStatusChanged : NotificationTypes.TaskUpdated,
                        Category = NotificationCategories.Task,
                        Title = statusChanged ? "Task status changed" : "Task updated",
                        Message = statusChanged
                            ? $"\"{task.Title}\" moved from {oldStatus} to {task.Status}."
                            : $"\"{task.Title}\" was updated.",
                        EntityName = "Task", EntityId = task.Id, Link = TaskLink(task.Id),
                        DedupeKey = Key("updated", task.Id)
                    });
                }

                _unitOfWork.Tasks.UpdateEntity(task);
                await _unitOfWork.SaveChanges();
                _unitOfWork.CommitTransaction();
                await _cache.BumpVersionAsync(CacheScopes.Dashboard);
                await _notifications.PublishQueuedAsync();

                _logger.LogInformation("Task action={Action} taskId={TaskId} userId={UserId} assignedTo={AssignedToUserId} status={Status}",
                    statusChanged ? AuditActions.StatusChange : assigneeChanged ? AuditActions.Assign : AuditActions.Update,
                    task.Id, currentUserId, task.AssignedToUserId, task.Status);

                var updatedDto = await _unitOfWork.Tasks.GetTaskDtoByIdAsync(task.Id);
                return ApiResponse<TaskDto>.Ok(updatedDto!, "Task updated successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                _logger.LogError(ex, "Error updating task action={Action} taskId={TaskId} userId={UserId}", AuditActions.Update, request.Id, currentUserId);
                return ApiResponse<TaskDto>.Fail("An error occurred while updating the task.", statusCode: 500);
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
                await _notifications.QueueAsync(new NotificationRequest
                {
                    RecipientIds = new[] { task.AssignedToUserId },
                    Type = NotificationTypes.TaskDeleted,
                    Category = NotificationCategories.Task,
                    Title = "Task removed",
                    Message = $"\"{task.Title}\" was deleted.",
                    EntityName = "Task", EntityId = task.Id, Link = null,
                    DedupeKey = Key("deleted", task.Id)
                });

                _unitOfWork.Tasks.DeleteEntity(task);
                await _unitOfWork.SaveChanges();
                _unitOfWork.CommitTransaction();
                await _cache.BumpVersionAsync(CacheScopes.Dashboard);
                await _notifications.PublishQueuedAsync();

                _logger.LogInformation("Task action={Action} taskId={TaskId} userId={UserId}", AuditActions.Delete, id, currentUserId);

                return ApiResponse<bool>.Ok(true, "Task deleted successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                _logger.LogError(ex, "Error deleting task action={Action} taskId={TaskId} userId={UserId}", AuditActions.Delete, id, currentUserId);
                return ApiResponse<bool>.Fail("An error occurred while deleting the task.", statusCode: 500);
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
                await Denied(id, "Attempted to change the status of a task that is not assigned to the user.");
                return ApiResponse<TaskDto>.Fail("You can only update the status of tasks assigned to you.", statusCode: 403);
            }

            var oldStatus = task.Status;
            task.Status = newStatus;
            task.ModifiedDate = DateTime.UtcNow;

            if (oldStatus != newStatus)
            {
                // Counterparts of the actor (assignee and creator) are informed; the actor is excluded by the service.
                await _notifications.QueueAsync(new NotificationRequest
                {
                    RecipientIds = new[] { task.AssignedToUserId, task.CreatedByUserId },
                    Type = NotificationTypes.TaskStatusChanged,
                    Category = NotificationCategories.Task,
                    Title = "Task status changed",
                    Message = $"\"{task.Title}\" moved from {oldStatus} to {newStatus}.",
                    EntityName = "Task", EntityId = task.Id, Link = TaskLink(task.Id),
                    DedupeKey = Key("status-" + newStatus.Replace(' ', '-'), task.Id)
                });
            }

            _unitOfWork.Tasks.UpdateEntity(task);
            await _unitOfWork.SaveChanges();
            await _cache.BumpVersionAsync(CacheScopes.Dashboard);
            await _notifications.PublishQueuedAsync();

            _logger.LogInformation("Task action={Action} taskId={TaskId} userId={UserId} oldStatus={OldStatus} newStatus={NewStatus}",
                AuditActions.StatusChange, task.Id, currentUserId, oldStatus, newStatus);

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
                await Denied(taskId, "Attempted to read comments of a task that is not assigned to the user.");
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
                await Denied(request.TaskId, "Attempted to comment on a task that is not assigned to the user.");
                return ApiResponse<TaskCommentDto>.Fail("You can only add comments to tasks assigned to you.", statusCode: 403);
            }

            var text = request.Comment.Trim();
            var comment = new TaskComment
            {
                TaskId = request.TaskId,
                UserId = currentUserId,
                Comment = text,
                CreatedDate = DateTime.UtcNow
            };

            _unitOfWork.TaskComments.AddEntity(comment);

            var preview = text.Length > 120 ? text[..120] + "…" : text;
            await _notifications.QueueAsync(new NotificationRequest
            {
                RecipientIds = new[] { task.AssignedToUserId, task.CreatedByUserId },
                Type = NotificationTypes.TaskCommented,
                Category = NotificationCategories.Comment,
                Title = "New comment on a task",
                Message = $"On \"{task.Title}\": {preview}",
                EntityName = "Task", EntityId = task.Id, Link = TaskLink(task.Id)
            });

            await _unitOfWork.SaveChanges();
            await _cache.BumpVersionAsync(CacheScopes.Dashboard);
            await _notifications.PublishQueuedAsync();

            _logger.LogInformation("Task action={Action} taskId={TaskId} userId={UserId} commentId={CommentId}",
                AuditActions.Comment, task.Id, currentUserId, comment.Id);

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

        public async Task<ApiResponse<PaginatedResult<AuditLogDto>>> GetTaskHistoryAsync(
            int taskId, AuditQueryParameters query, int currentUserId, string currentUserRole)
        {
            var isAdmin = currentUserRole == RoleNames.Admin;
            var task = await _unitOfWork.Tasks.GetTaskEntityByIdAsync(taskId, includeDetails: false);

            if (task == null && !isAdmin)
            {
                return ApiResponse<PaginatedResult<AuditLogDto>>.Fail("Task not found.", statusCode: 404);
            }

            if (task != null && !isAdmin && task.AssignedToUserId != currentUserId)
            {
                await Denied(taskId, "Attempted to read the history of a task that is not assigned to the user.");
                return ApiResponse<PaginatedResult<AuditLogDto>>.Fail("You are not authorized to view the history of this task.", statusCode: 403);
            }

            var result = await _unitOfWork.AuditLogs.GetTaskHistoryAsync(taskId, query, includeSensitive: isAdmin);
            return ApiResponse<PaginatedResult<AuditLogDto>>.Ok(result);
        }

        private Task Denied(int taskId, string description)
        {
            _logger.LogWarning("Authorization denied action={Action} taskId={TaskId} reason={Reason}", AuditActions.AccessDenied, taskId, description);
            return _audit.LogAsync(AuditActions.AccessDenied, "Task", taskId.ToString(), false, description, relatedTaskId: taskId);
        }

        private static string TaskLink(int taskId) => $"/tasks?open={taskId}";

        // Same event for the same record within the same minute is considered a duplicate.
        private static string Key(string evt, int taskId) => $"task:{taskId}:{evt}:{DateTime.UtcNow:yyyyMMddHHmm}";
    }
}
