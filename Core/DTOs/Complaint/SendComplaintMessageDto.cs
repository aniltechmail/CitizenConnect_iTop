using Core.Enums;

namespace Core.DTOs.Complaint
{
    public class SendComplaintMessageDto
    {
        public string Message { get; set; } = string.Empty;
        public SenderType? SenderType { get; set; }
    }
}
