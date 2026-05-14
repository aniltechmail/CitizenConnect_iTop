namespace Core.DTOs.ITop
{
    public class ITopAttachmentCreateResult
    {
        public bool WasAttempted { get; set; }
        public bool Success { get; set; }
        public string? AttachmentId { get; set; }
        public string? Error { get; set; }

        public static ITopAttachmentCreateResult Skipped(string reason) => new()
        {
            WasAttempted = false,
            Success = false,
            Error = reason
        };

        public static ITopAttachmentCreateResult Failed(string error) => new()
        {
            WasAttempted = true,
            Success = false,
            Error = error
        };
    }
}
