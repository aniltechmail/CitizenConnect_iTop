namespace Core.Entities.Mobile
{
    public class CitizenRefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CitizenId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
