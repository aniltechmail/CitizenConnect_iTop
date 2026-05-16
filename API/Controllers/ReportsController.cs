using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin,Assigner,Supervisor,FieldAgent,DepartmentHead,TopManagement")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public ReportsController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetComplaints(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] ComplaintStatus? status,
            [FromQuery] int? departmentId,
            [FromQuery] int? districtId,
            [FromQuery] int? constituencyId,
            [FromQuery] int? areaId,
            [FromQuery] int? blockId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _reportingService.GetComplaintReportAsync(
                fromDate, toDate, status, departmentId, districtId, constituencyId, areaId, blockId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("complaints/export")]
        public async Task<IActionResult> ExportComplaints(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] ComplaintStatus? status,
            [FromQuery] int? departmentId,
            [FromQuery] int? districtId,
            [FromQuery] int? constituencyId,
            [FromQuery] int? areaId,
            [FromQuery] int? blockId)
        {
            var csv = await _reportingService.ExportComplaintReportCsvAsync(
                fromDate, toDate, status, departmentId, districtId, constituencyId, areaId, blockId);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "complaints-report.csv");
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            return Ok(await _reportingService.GetDepartmentPerformanceAsync());
        }

        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            return Ok(await _reportingService.GetAgentPerformanceAsync());
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            return Ok(await _reportingService.GetLocationReportAsync());
        }
    }
}
