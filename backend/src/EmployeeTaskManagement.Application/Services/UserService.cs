using AutoMapper;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Application.Interfaces.Security;
using EmployeeTaskManagement.Core.Entities;
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

        public UserService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAllUsersAsync();
            return ApiResponse<IEnumerable<UserDto>>.Ok(users.OrderBy(u => u.FullName));
        }

        public async Task<ApiResponse<IEnumerable<UserDto>>> GetEmployeesAsync()
        {
            var employees = await _unitOfWork.Users.GetEmployeesAsync();
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

                // Fetch created user with role
                var createdUser = await _unitOfWork.Users.GetUserEntityByIdAsync(user.Id, includeRole: true);
                return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(createdUser), "User created successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return ApiResponse<UserDto>.Fail($"Error creating user: {ex.Message}", statusCode: 500);
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

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(request.Password);
                }

                _unitOfWork.Users.UpdateEntity(user);
                await _unitOfWork.SaveChanges();
                _unitOfWork.CommitTransaction();

                var updatedUser = await _unitOfWork.Users.GetUserEntityByIdAsync(user.Id, includeRole: true);
                return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(updatedUser), "User updated successfully.");
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return ApiResponse<UserDto>.Fail($"Error updating user: {ex.Message}", statusCode: 500);
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

            string status = user.IsActive ? "activated" : "deactivated";
            return ApiResponse<bool>.Ok(user.IsActive, $"User has been successfully {status}.");
        }

        public async Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync()
        {
            var roles = await _unitOfWork.Roles.GetAllRolesAsync();
            return ApiResponse<IEnumerable<RoleDto>>.Ok(roles);
        }
    }
}
