using EmployeeTaskManagement.API.Services;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Logging;
using EmployeeTaskManagement.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeTaskManagement.API.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class LogsController : BaseApiController
    {
        private readonly ILogQueryService _logs;

        public LogsController(ILogQueryService logs) => _logs = logs;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] SystemLogQueryParameters query, CancellationToken ct)
        {
            var result = await _logs.QueryAsync(query, ct);
            return Ok(ApiResponse<PaginatedResult<SystemLogEntryDto>>.Ok(result));
        }
    }
}
