using Core.Enums;
using Core.Interfaces.Services;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(AppDbContext db, ILogger<PushNotificationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SendAsync(SenderType userType, Guid userId, string title, string body, Guid? complaintId = null)
        {
            var tokens = await _db.MobileDevices
                .Where(x => x.UserType == userType && x.UserId == userId && x.IsActive)
                .Select(x => x.DeviceToken)
                .ToListAsync();

            if (tokens.Count == 0)
                return;

            _logger.LogInformation(
                "FCM push queued for {Count} device(s). Title: {Title}; ComplaintId: {ComplaintId}",
                tokens.Count,
                title,
                complaintId);
        }

        public async Task SendManyAsync(SenderType userType, IEnumerable<Guid> userIds, string title, string body, Guid? complaintId = null)
        {
            foreach (var userId in userIds.Distinct())
                await SendAsync(userType, userId, title, body, complaintId);
        }
    }
}
