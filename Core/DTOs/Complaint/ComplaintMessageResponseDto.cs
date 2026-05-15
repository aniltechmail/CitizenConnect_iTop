namespace Core.DTOs.Complaint
{
    public class ComplaintMessageResponseDto
    {
        public Guid Id { get; set; }
        public Guid ComplaintId { get; set; }
        public string SenderType { get; set; } = string.Empty;
        public Guid SenderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
