using Core.DTOs.Notification;
using Core.Enums;

namespace Core.Interfaces.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponseDto>> GetMyNotificationsAsync(SenderType userType, Guid userId);
        Task<NotificationResponseDto> MarkAsReadAsync(Guid notificationId, SenderType userType, Guid userId);
    }
}
