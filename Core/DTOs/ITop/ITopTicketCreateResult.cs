namespace Core.DTOs.ITop
{
    public class ITopTicketCreateResult
    {
        public bool WasAttempted { get; set; }
        public bool Success { get; set; }
        public string? TicketId { get; set; }
        public string? TicketRef { get; set; }
        public string? Error { get; set; }

        public static ITopTicketCreateResult Skipped(string reason) => new()
        {
            WasAttempted = false,
            Success = false,
            Error = reason
        };

        public static ITopTicketCreateResult Failed(string error) => new()
        {
            WasAttempted = true,
            Success = false,
            Error = error
        };
    }
}
