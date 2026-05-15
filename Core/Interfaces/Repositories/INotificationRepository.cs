using Core.Entities;
using Core.Enums;

namespace Core.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task AddRangeAsync(IEnumerable<Notification> notifications);
        Task<IEnumerable<Notification>> GetByUserAsync(SenderType userType, Guid userId);
        Task<Notification?> GetByIdAsync(Guid id);
        Task UpdateAsync(Notification notification);
    }
}
