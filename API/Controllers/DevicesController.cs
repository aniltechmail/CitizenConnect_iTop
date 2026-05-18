using Core.DTOs.Mobile;
using Core.Entities.Mobile;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/devices")]
    [Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DevicesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceToken))
                return BadRequest(new { message = "Device token is required." });

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userType = User.FindFirstValue("user_type") == "citizen"
                ? SenderType.Citizen
                : SenderType.Agent;

            var existing = await _db.MobileDevices
                .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceToken == request.DeviceToken);

            if (existing == null)
            {
                existing = new MobileDevice
                {
                    UserId = userId,
                    UserType = userType,
                    DeviceToken = request.DeviceToken
                };
                _db.MobileDevices.Add(existing);
            }

            existing.Platform = request.Platform;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { registered = true, existing.Id });
        }
    }
}
