using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeTaskManagement.Application.DTOs.Users;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Enums;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var result = await _userService.GetEmployeesAsync();
            return Ok(result);
        }

        [HttpGet("roles")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _userService.GetRolesAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            // Employees can only view their own profile; Admins can view any user
            if (CurrentUserRole == RoleNames.Employee && CurrentUserId != id)
            {
                return Forbid();
            }

            var result = await _userService.GetUserByIdAsync(id);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto request)
        {
            var result = await _userService.CreateUserAsync(request);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return CreatedAtAction(nameof(GetUserById), new { id = result.Data!.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto request)
        {
            request.Id = id;
            var result = await _userService.UpdateUserAsync(request);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var result = await _userService.ToggleUserStatusAsync(id, CurrentUserId);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
