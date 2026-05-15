namespace Core.DTOs.Escalation
{
    public class EscalationEventResponseDto
    {
        public Guid Id { get; set; }
        public Guid ComplaintId { get; set; }
        public string ComplaintRefNumber { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
