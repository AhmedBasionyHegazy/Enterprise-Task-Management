using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeTaskManagement.Application.Services;
using System.Threading.Tasks;

namespace EmployeeTaskManagement.API.Controllers
{
    [Authorize]
    public class DashboardController : BaseApiController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _dashboardService.GetDashboardSummaryAsync(CurrentUserId, CurrentUserRole);
            return Ok(result);
        }
    }
}
