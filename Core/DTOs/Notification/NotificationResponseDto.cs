namespace Core.DTOs.Notification
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string UserType { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid? ComplaintId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
