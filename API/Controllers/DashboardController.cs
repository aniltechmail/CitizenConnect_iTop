using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = "Admin,Assigner,Supervisor,FieldAgent,DepartmentHead,TopManagement")]
    public class DashboardController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public DashboardController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            return Ok(await _reportingService.GetDashboardSummaryAsync());
        }
    }
}
