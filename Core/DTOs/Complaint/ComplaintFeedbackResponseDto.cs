namespace Core.DTOs.Complaint
{
    public class ComplaintFeedbackResponseDto
    {
        public Guid Id { get; set; }
        public Guid ComplaintId { get; set; }
        public Guid CitizenId { get; set; }
        public int Rating { get; set; }
        public string? Comments { get; set; }
        public Guid? CollectedById { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
