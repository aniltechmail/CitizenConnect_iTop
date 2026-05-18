namespace Core.Entities.Mobile
{
    public class CitizenOtp
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Phone { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public int VerifyAttempts { get; set; }
        public int ResendCount { get; set; }
        public DateTime? LastResendOn { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
