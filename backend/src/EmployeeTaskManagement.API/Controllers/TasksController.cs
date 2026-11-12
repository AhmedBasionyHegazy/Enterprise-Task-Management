using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeTaskManagement.Application.DTOs.Comments;
using EmployeeTaskManagement.Application.DTOs.Tasks;
using EmployeeTaskManagement.Application.Services;
using EmployeeTaskManagement.Core.Enums;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.API.Controllers
{
    [Authorize]
    public class TasksController : BaseApiController
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] TaskQueryParameters query)
        {
            var result = await _taskService.GetTasksAsync(query, CurrentUserId, CurrentUserRole);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var result = await _taskService.GetTaskByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto request)
        {
            var result = await _taskService.CreateTaskAsync(request, CurrentUserId);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return CreatedAtAction(nameof(GetTaskById), new { id = result.Data!.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto request)
        {
            request.Id = id;
            var result = await _taskService.UpdateTaskAsync(request, CurrentUserId);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _taskService.DeleteTaskAsync(id, CurrentUserId);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] TaskStatusUpdateDto request)
        {
            var result = await _taskService.UpdateTaskStatusAsync(id, request.Status, CurrentUserId, CurrentUserRole);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var result = await _taskService.GetTaskCommentsAsync(id, CurrentUserId, CurrentUserRole);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] TaskCommentCreateDto request)
        {
            request.TaskId = id;
            var result = await _taskService.AddCommentAsync(request, CurrentUserId, CurrentUserRole);
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
