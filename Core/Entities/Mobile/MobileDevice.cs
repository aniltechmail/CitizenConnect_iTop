using Core.Enums;

namespace Core.Entities.Mobile
{
    public class MobileDevice
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public SenderType UserType { get; set; }
        public Guid UserId { get; set; }
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
