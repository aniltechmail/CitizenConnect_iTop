using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/sync")]
    public class SyncController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SyncController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("location")]
        public async Task<IActionResult> GetLocationHierarchy()
        {
            var districts = await _db.Districts
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Code,
                    Constituencies = d.Constituencies
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Code,
                            Areas = c.Areas
                                .Where(a => a.IsActive)
                                .OrderBy(a => a.Name)
                                .Select(a => new
                                {
                                    a.Id,
                                    a.Name,
                                    a.Code,
                                    Blocks = a.Blocks
                                        .Where(b => b.IsActive)
                                        .OrderBy(b => b.Name)
                                        .Select(b => new { b.Id, b.Name, b.Code })
                                })
                        })
                })
                .ToListAsync();

            return Ok(new { syncedAt = DateTime.UtcNow, districts });
        }
    }
}
