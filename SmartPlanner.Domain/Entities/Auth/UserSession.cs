using System;

namespace SmartPlanner.Domain.Entities
{
    public class UserSession : BaseEntity
    {
        public Guid UserId { get; set; }
        public string RefreshTokenHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsRevoked { get; set; }

        public virtual User User { get; set; } = null!;

        public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;
        public bool IsExpired() => ExpiresAt <= DateTime.UtcNow;
    }
}
