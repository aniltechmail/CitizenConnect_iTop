namespace Core.DTOs.Mobile
{
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class SendOtpRequestDto
    {
        public string Phone { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
    }

    public class SendOtpResponseDto
    {
        public string Phone { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime ExpiresOn { get; set; }
        public string? DevOtp { get; set; }
        public string? Otp { get; set; }
        public bool IsNewOtp { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VerifyOtpRequestDto
    {
        public string Phone { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public string Otp { get; set; } = string.Empty;
    }

    public class RegisterDeviceRequestDto
    {
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }
}
