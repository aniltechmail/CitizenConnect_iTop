using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;
        public NotificationRepository(AppDbContext db) => _db = db;

        public async Task AddAsync(Notification notification)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Notification> notifications)
        {
            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Notification>> GetByUserAsync(SenderType userType, Guid userId) =>
            await _db.Notifications
                .Where(n => n.UserType == userType && n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task<Notification?> GetByIdAsync(Guid id) =>
            await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);

        public async Task UpdateAsync(Notification notification)
        {
            _db.Notifications.Update(notification);
            await _db.SaveChangesAsync();
        }
    }
}
