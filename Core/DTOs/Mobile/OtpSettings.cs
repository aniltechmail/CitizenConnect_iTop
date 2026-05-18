namespace Core.DTOs.Mobile
{
    public class OtpSettings
    {
        public int ExpiryMinutes { get; set; } = 5;
        public int MaxVerifyAttempts { get; set; } = 5;
        public int ResendCooldownSeconds { get; set; } = 60;
        public int MaxResendsPerDay { get; set; } = 25;
        public List<FixedOtpUser> FixedOtpUsers { get; set; } = new();
    }

    public class FixedOtpUser
    {
        public string MobileNumber { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
    }

    public class SmsProviderOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string AuthKey { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public string SmsContentType { get; set; } = "english";
    }
}
