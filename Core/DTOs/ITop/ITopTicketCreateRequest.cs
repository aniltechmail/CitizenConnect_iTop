namespace Core.DTOs.ITop
{
    public class ITopTicketCreateRequest
    {
        public Guid ComplaintId { get; set; }
        public string RefNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CitizenName { get; set; } = string.Empty;
        public string CitizenPhone { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BlockName { get; set; } = string.Empty;
        public int Priority { get; set; }
    }
}
