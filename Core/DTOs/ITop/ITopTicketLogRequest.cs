namespace Core.DTOs.ITop
{
    public class ITopTicketLogRequest
    {
        public string ITopTicketId { get; set; } = string.Empty;
        public string ITopClass { get; set; } = "UserRequest";
        public string ComplaintRefNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
    }
}
