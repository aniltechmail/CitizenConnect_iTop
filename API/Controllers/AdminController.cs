using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.Location;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers() =>
            Ok(await _db.InternalUsers.OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName, x.Email, Role = x.Role.ToString(), x.DepartmentId, x.IsActive })
                .ToListAsync());

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] InternalUserRequest request)
        {
            if (await _db.InternalUsers.AnyAsync(x => x.Email == request.Email))
                return Conflict(new { message = "Email already exists." });

            var user = new InternalUser
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = (UserRole)request.Role,
                DepartmentId = request.DepartmentId,
                IsActive = request.IsActive
            };
            _db.InternalUsers.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { user.Id, user.FullName, user.Email, Role = user.Role.ToString(), user.DepartmentId, user.IsActive });
        }

        [HttpPut("users/{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] InternalUserRequest request)
        {
            var user = await _db.InternalUsers.FindAsync(id);
            if (user == null) return NotFound();
            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Role = (UserRole)request.Role;
            user.DepartmentId = request.DepartmentId;
            user.IsActive = request.IsActive;
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _db.InternalUsers.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments() =>
            Ok(await _db.Departments.OrderBy(x => x.Name).ToListAsync());

        [HttpPost("departments")]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentRequest request)
        {
            var item = new Department { Name = request.Name, Code = request.Code, Description = request.Description, IsActive = request.IsActive };
            _db.Departments.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("departments/{id:int}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DepartmentRequest request)
        {
            var item = await _db.Departments.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = request.Name;
            item.Code = request.Code;
            item.Description = request.Description;
            item.IsActive = request.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("departments/{id:int}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var item = await _db.Departments.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories() =>
            Ok(await _db.ComplaintCategories.Include(x => x.Department).OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Description, x.DepartmentId, DepartmentName = x.Department.Name, x.IsActive })
                .ToListAsync());

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryRequest request)
        {
            var item = new ComplaintCategory { Name = request.Name, Description = request.Description, DepartmentId = request.DepartmentId, IsActive = request.IsActive };
            _db.ComplaintCategories.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("categories/{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequest request)
        {
            var item = await _db.ComplaintCategories.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = request.Name;
            item.Description = request.Description;
            item.DepartmentId = request.DepartmentId;
            item.IsActive = request.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var item = await _db.ComplaintCategories.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("sla-policies")]
        public async Task<IActionResult> GetSlaPolicies() =>
            Ok(await _db.SlaPolicies.Include(x => x.Category).OrderBy(x => x.Category.Name)
                .Select(x => new
                {
                    x.Id,
                    x.CategoryId,
                    CategoryName = x.Category.Name,
                    x.ResponseHours,
                    x.ResolutionHours,
                    x.EscalationLevel1Hours,
                    x.EscalationLevel2Hours,
                    x.EscalationLevel3Hours,
                    x.IsActive
                }).ToListAsync());

        [HttpPost("sla-policies")]
        public async Task<IActionResult> CreateSlaPolicy([FromBody] SlaPolicyRequest request)
        {
            var item = new SlaPolicy
            {
                CategoryId = request.CategoryId,
                ResponseHours = request.ResponseHours,
                ResolutionHours = request.ResolutionHours,
                EscalationLevel1Hours = request.EscalationLevel1Hours,
                EscalationLevel2Hours = request.EscalationLevel2Hours,
                EscalationLevel3Hours = request.EscalationLevel3Hours,
                IsActive = request.IsActive
            };
            _db.SlaPolicies.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("sla-policies/{id:int}")]
        public async Task<IActionResult> UpdateSlaPolicy(int id, [FromBody] SlaPolicyRequest request)
        {
            var item = await _db.SlaPolicies.FindAsync(id);
            if (item == null) return NotFound();
            item.CategoryId = request.CategoryId;
            item.ResponseHours = request.ResponseHours;
            item.ResolutionHours = request.ResolutionHours;
            item.EscalationLevel1Hours = request.EscalationLevel1Hours;
            item.EscalationLevel2Hours = request.EscalationLevel2Hours;
            item.EscalationLevel3Hours = request.EscalationLevel3Hours;
            item.IsActive = request.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("sla-policies/{id:int}")]
        public async Task<IActionResult> DeleteSlaPolicy(int id)
        {
            var item = await _db.SlaPolicies.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("locations/districts")]
        public async Task<IActionResult> GetDistricts() => Ok(await _db.Districts.OrderBy(x => x.Name).ToListAsync());

        [HttpPost("locations/districts")]
        public async Task<IActionResult> CreateDistrict([FromBody] LocationRequest request)
        {
            var item = new District { Name = request.Name, Code = request.Code, IsActive = request.IsActive };
            _db.Districts.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("locations/districts/{id:int}")]
        public async Task<IActionResult> UpdateDistrict(int id, [FromBody] LocationRequest request)
        {
            var item = await _db.Districts.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = request.Name;
            item.Code = request.Code;
            item.IsActive = request.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("locations/districts/{id:int}")]
        public async Task<IActionResult> DeleteDistrict(int id)
        {
            var item = await _db.Districts.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("locations/constituencies")]
        public async Task<IActionResult> GetConstituencies() => Ok(await _db.Constituencies.OrderBy(x => x.Name).ToListAsync());

        [HttpPost("locations/constituencies")]
        public async Task<IActionResult> CreateConstituency([FromBody] ConstituencyRequest request)
        {
            var item = new Constituency { Name = request.Name, Code = request.Code, DistrictId = request.DistrictId, IsActive = request.IsActive };
            _db.Constituencies.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("locations/constituencies/{id:int}")]
        public async Task<IActionResult> UpdateConstituency(int id, [FromBody] ConstituencyRequest request)
        {
            var item = await _db.Constituencies.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = request.Name;
            item.Code = request.Code;
            item.DistrictId = request.DistrictId;
            item.IsActive = request.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("locations/constituencies/{id:int}")]
        public async Task<IActionResult> DeleteConstituency(int id)
        {
            var item = await _db.Constituencies.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("locations/areas")]
        public async Task<IActionResult> GetAreas() => Ok(await _db.Areas.OrderBy(x => x.Name).ToListAsync());

        [HttpPost("locations/areas")]
        public async Task<IActionResult> CreateArea([FromBody] AreaRequest request)
        {
            var item = new Area { Name = request.Name, Code = request.Code, ConstituencyId = request.ConstituencyId, IsActive = request.IsActive };
            _db.Areas.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("locations/areas/{id:int}")]
        public async Task<IActionResult> UpdateArea(int id, [FromBody] AreaRequest request)
        {
            var item = await _db.Areas.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = request.Name;
            item.Code = request.Code;
            item.ConstituencyId = request.ConstituencyId;
            item.IsActive = request.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("locations/areas/{id:int}")]
        public async Task<IActionResult> DeleteArea(int id)
        {
            var item = await _db.Areas.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("locations/blocks")]
        public async Task<IActionResult> GetBlocks() => Ok(await _db.Blocks.OrderBy(x => x.Name).ToListAsync());

        [HttpPost("locations/blocks")]
        public async Task<IActionResult> CreateBlock([FromBody] BlockRequest request)
        {
            var item = new Block { Name = request.Name, Code = request.Code, AreaId = request.AreaId, IsActive = request.IsActive };
            _db.Blocks.Add(item);
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpPut("locations/blocks/{id:int}")]
        public async Task<IActionResult> UpdateBlock(int id, [FromBody] BlockRequest request)
        {
            var item = await _db.Blocks.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = request.Name;
            item.Code = request.Code;
            item.AreaId = request.AreaId;
            item.IsActive = request.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("locations/blocks/{id:int}")]
        public async Task<IActionResult> DeleteBlock(int id)
        {
            var item = await _db.Blocks.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    public class InternalUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int Role { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CategoryRequest : DepartmentRequest
    {
        public int DepartmentId { get; set; }
    }

    public class SlaPolicyRequest
    {
        public int CategoryId { get; set; }
        public int ResponseHours { get; set; }
        public int ResolutionHours { get; set; }
        public int EscalationLevel1Hours { get; set; }
        public int EscalationLevel2Hours { get; set; }
        public int EscalationLevel3Hours { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LocationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class ConstituencyRequest : LocationRequest
    {
        public int DistrictId { get; set; }
    }

    public class AreaRequest : LocationRequest
    {
        public int ConstituencyId { get; set; }
    }

    public class BlockRequest : LocationRequest
    {
        public int AreaId { get; set; }
    }
}
