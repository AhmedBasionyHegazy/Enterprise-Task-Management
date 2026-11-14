using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Caching;
using EmployeeTaskManagement.Application.Interfaces.Notifications;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.Application.Services
{
    public interface IUserService
    {
        Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
        Task<ApiResponse<IEnumerable<UserDto>>> GetEmployeesAsync();
        Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
        Task<ApiResponse<UserDto>> CreateUserAsync(UserCreateDto request);
        Task<ApiResponse<UserDto>> UpdateUserAsync(UserUpdateDto request);
        Task<ApiResponse<bool>> ToggleUserStatusAsync(int id, int currentUserId);
        Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync();
    }

    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly INotificationService _notifications;
        private readonly ITokenSessionService _sessions;
        private readonly ICacheService _cache;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IMapper mapper,
            INotificationService notifications,
            ITokenSessionService sessions,
            ICacheService cache,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _notifications = notifications;
            _sessions = sessions;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAllUsersAsync();
            return ApiResponse<IEnumerable<UserDto>>.Ok(users.OrderBy(u => u.FullName));
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetEmployeesAsync()
        {
            var version = await _cache.GetVersionAsync(CacheScopes.Users);
            var employees = await _cache.GetOrCreateAsync(
                $"users:employees:v{version}",
                async _ => (await _unitOfWork.Users.GetEmployeesAsync()).ToList(),
                TimeSpan.FromMinutes(5));

            return ApiResponse<IEnumerable<UserDto>>.Ok(employees);
        }

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
        {
            var user = await _unitOfWork.Users.GetUserEntityByIdAsync(id, includeRole: true);
            if (user == null)
            {
                return ApiResponse<UserDto>.Fail("User not found.", statusCode: 404);
            }

            return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(user));
        }

        public async Task<ApiResponse<UserDto>> CreateUserAsync(UserCreateDto request)
        {
            // 1. Validation checks
            if (await _unitOfWork.Users.IsUsernameTakenAsync(request.Username))
            {
                return ApiResponse<UserDto>.Fail("Username is already taken by another account.", statusCode: 400);
            }

            if (await _unitOfWork.Users.IsEmailTakenAsync(request.Email))
            {
                return ApiResponse<UserDto>.Fail("Email address is already registered.", statusCode: 400);
            }

            _unitOfWork.StartTransaction();
            try
            {
                var user = _mapper.Map<User>(request);
                user.PasswordHash = _passwordHasher.HashPassword(request.Password);
                user.IsActive = true;
                user.CreatedDate = DateTime.UtcNow;

                _unitOfWork.Users.AddEntity(user);
                await _unitOfWork.SaveChanges();

                _unitOfWork.CommitTransaction();

                await InvalidateUserCachesAsync(user.Id);
                _logger.LogInformation("User action={Action} targetUserId={TargetUserId} username={Username} roleId={RoleId}",
                    AuditActions.Create, user.Id, user.Username, user.RoleId);

                await NotifyAdminsAsync(NotificationTypes.UserCreated, "New employee account",
                    $"Account for {user.FullName} ({user.Username}) was created.", user.Id);

                // Fetch created user with role
                var createdUser = await _unitOfWork.Users.GetUserEntityByIdAsync(user.Id, includeRole: true);
                return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(createdUser), "User created successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                _logger.LogError(ex, "Error creating user action={Action} username={Username}", AuditActions.Create, request.Username);
                return ApiResponse<UserDto>.Fail("An error occurred while creating the user.", statusCode: 500);
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(UserUpdateDto request)
        {
            var user = await _unitOfWork.Users.GetUserEntityByIdAsync(request.Id, includeRole: true);
            if (user == null)
            {
                return ApiResponse<UserDto>.Fail("User not found.", statusCode: 404);
            }

            if (await _unitOfWork.Users.IsEmailTakenAsync(request.Email, excludeUserId: request.Id))
            {
                return ApiResponse<UserDto>.Fail("Email address is already in use by another user.", statusCode: 400);
            }

            var wasActive = user.IsActive;
            var oldRoleId = user.RoleId;
            var passwordChanged = !string.IsNullOrWhiteSpace(request.Password);

            _unitOfWork.StartTransaction();
            try
            {
                user.FullName = request.FullName.Trim();
                user.Email = request.Email.Trim();
                user.Department = request.Department?.Trim();
                user.RoleId = request.RoleId;
                user.ModifiedDate = DateTime.UtcNow;

                if (request.IsActive.HasValue)
                {
                    user.IsActive = request.IsActive.Value;
                }

                if (passwordChanged)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(request.Password!);
                }

                _unitOfWork.Users.UpdateEntity(user);
                await _unitOfWork.SaveChanges();
                _unitOfWork.CommitTransaction();

                var becameInactive = wasActive && !user.IsActive;
                var roleChanged = oldRoleId != user.RoleId;
                if (becameInactive || roleChanged || passwordChanged)
                {
                    var reason = becameInactive ? "AccountDeactivated"
                        : passwordChanged ? "PasswordChanged" : "RoleChanged";
                    await _sessions.RevokeAllForUserAsync(user.Id, reason);
                }

                await InvalidateUserCachesAsync(user.Id);
                _logger.LogInformation("User action={Action} targetUserId={TargetUserId} roleChanged={RoleChanged} passwordChanged={PasswordChanged} isActive={IsActive}",
                    AuditActions.Update, user.Id, roleChanged, passwordChanged, user.IsActive);

                var updatedUser = await _unitOfWork.Users.GetUserEntityByIdAsync(user.Id, includeRole: true);
                return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(updatedUser), "User updated successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                _logger.LogError(ex, "Error updating user action={Action} targetUserId={TargetUserId}", AuditActions.Update, request.Id);
                return ApiResponse<UserDto>.Fail("An error occurred while updating the user.", statusCode: 500);
            }
        }

        public async Task<ApiResponse<bool>> ToggleUserStatusAsync(int id, int currentUserId)
        {
            if (id == currentUserId)
            {
                return ApiResponse<bool>.Fail("You cannot deactivate your own account.", statusCode: 400);
            }

            var user = await _unitOfWork.Users.GetUserEntityByIdAsync(id, includeRole: false);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("User not found.", statusCode: 404);
            }

            user.IsActive = !user.IsActive;
            user.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Users.UpdateEntity(user);
            await _unitOfWork.SaveChanges();

            if (!user.IsActive)
            {
                await _sessions.RevokeAllForUserAsync(user.Id, "AccountDeactivated");
            }

            await InvalidateUserCachesAsync(user.Id);

            string status = user.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("User action={Action} targetUserId={TargetUserId} status={Status}",
                AuditActions.Update, user.Id, status);

            await NotifyAdminsAsync(NotificationTypes.UserStatusChanged, "Account status changed",
                $"{user.FullName} was {status}.", user.Id);
            return ApiResponse<bool>.Ok(user.IsActive, $"User has been successfully {status}.");
        }

        public async Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync()
        {
            var roles = await _cache.GetOrCreateAsync(
                "roles:all",
                async _ => (await _unitOfWork.Roles.GetAllRolesAsync()).ToList(),
                TimeSpan.FromMinutes(30));

            return ApiResponse<IEnumerable<RoleDto>>.Ok(roles);
        }

        private async Task InvalidateUserCachesAsync(int userId)
        {
            await _cache.RemoveAsync($"auth:active:{userId}");
            await _cache.BumpVersionAsync(CacheScopes.Users);
            await _cache.BumpVersionAsync(CacheScopes.Dashboard);
        }

        private async Task NotifyAdminsAsync(string type, string title, string message, int userId)
        {
            var admins = await _unitOfWork.Users.GetActiveAdminIdsAsync();
            if (admins == null || admins.Count == 0) return;

            await _notifications.QueueAsync(new NotificationRequest
            {
                RecipientIds = admins,
                Type = type,
                Category = NotificationCategories.Admin,
                Title = title,
                Message = message,
                EntityName = "User",
                EntityId = userId,
                Link = "/employees",
                DedupeKey = $"user:{userId}:{type}:{DateTime.UtcNow:yyyyMMddHHmm}"
            });
            await _unitOfWork.SaveChanges();
            await _notifications.PublishQueuedAsync();
        }
    }
}
