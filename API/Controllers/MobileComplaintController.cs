using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/complaint")]
    [Authorize]
    public class MobileComplaintController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MobileComplaintController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{id:guid}/media/{mediaId:guid}")]
        public async Task<IActionResult> DownloadMedia(Guid id, Guid mediaId)
        {
            var complaint = await _db.Complaints.Include(x => x.Media)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (complaint == null)
                return NotFound(new { message = "Complaint not found." });

            var userType = User.FindFirstValue("user_type");
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (userType != "internal" && complaint.CitizenId != userId)
                return Forbid();

            var media = complaint.Media.FirstOrDefault(x => x.Id == mediaId);
            if (media == null)
                return NotFound(new { message = "Media not found." });

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", media.FilePath);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "File not found on disk." });

            return PhysicalFile(filePath, media.MimeType ?? "application/octet-stream", media.FileName, enableRangeProcessing: true);
        }
    }
}
