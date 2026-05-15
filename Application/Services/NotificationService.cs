using Core.DTOs.Notification;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;

        public NotificationService(INotificationRepository notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetMyNotificationsAsync(
            SenderType userType,
            Guid userId)
        {
            var notifications = await _notificationRepo.GetByUserAsync(userType, userId);
            return notifications.Select(MapToDto);
        }

        public async Task<NotificationResponseDto> MarkAsReadAsync(
            Guid notificationId,
            SenderType userType,
            Guid userId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId)
                ?? throw new KeyNotFoundException("Notification not found.");

            if (notification.UserType != userType || notification.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this notification.");

            notification.IsRead = true;
            await _notificationRepo.UpdateAsync(notification);
            return MapToDto(notification);
        }

        private static NotificationResponseDto MapToDto(Core.Entities.Notification n) => new()
        {
            Id = n.Id,
            UserType = n.UserType.ToString(),
            UserId = n.UserId,
            ComplaintId = n.ComplaintId,
            Type = n.Type.ToString(),
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
