using Core.Enums;

namespace Core.Interfaces.Services
{
    public interface IPushNotificationService
    {
        Task SendAsync(SenderType userType, Guid userId, string title, string body, Guid? complaintId = null);
        Task SendManyAsync(SenderType userType, IEnumerable<Guid> userIds, string title, string body, Guid? complaintId = null);
    }
}
