using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/master")]
    public class MasterController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MasterController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments() =>
            Ok(await _db.Departments
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code, x.Description, x.IsActive })
                .ToListAsync());

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories() =>
            Ok(await _db.ComplaintCategories
                .Include(x => x.Department)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description,
                    x.DepartmentId,
                    DepartmentName = x.Department.Name,
                    x.IsActive
                })
                .ToListAsync());

        [HttpGet("blocks")]
        public async Task<IActionResult> GetBlocks() =>
            Ok(await _db.Blocks
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code, x.AreaId, x.IsActive })
                .ToListAsync());

        [HttpGet("agents")]
        [Authorize(Roles = "Admin,Assigner,Supervisor,FieldAgent,DepartmentHead,TopManagement")]
        public async Task<IActionResult> GetAgents() =>
            Ok(await _db.InternalUsers
                .Where(x => x.IsActive && x.Role == Core.Enums.UserRole.FieldAgent)
                .OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName, x.Email, x.DepartmentId })
                .ToListAsync());
    }
}
