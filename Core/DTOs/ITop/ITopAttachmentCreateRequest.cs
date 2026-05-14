namespace Core.DTOs.ITop
{
    public class ITopAttachmentCreateRequest
    {
        public string ITopTicketId { get; set; } = string.Empty;
        public string ITopClass { get; set; } = "UserRequest";
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ComplaintRefNumber { get; set; } = string.Empty;
    }
}
