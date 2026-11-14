using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeTaskManagement.Application.Common;
using EmployeeTaskManagement.Application.DTOs.Audit;
using EmployeeTaskManagement.Application.Interfaces.Persistence;
using EmployeeTaskManagement.Core.Enums;

namespace EmployeeTaskManagement.API.Controllers
{
    /// <summary>Read-only. There is intentionally no endpoint that can create, modify or delete audit records.</summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class AuditController : BaseApiController
    {
        private readonly IUnitOfWork _uow;

        public AuditController(IUnitOfWork uow) => _uow = uow;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] AuditQueryParameters query)
        {
            var result = await _uow.AuditLogs.QueryAsync(query, includeSensitive: true);
            return Ok(ApiResponse<PaginatedResult<AuditLogDto>>.Ok(result));
        }

        [HttpGet("actions")]
        public IActionResult Actions() => Ok(ApiResponse<string[]>.Ok(AuditActions.All));
    }
}
