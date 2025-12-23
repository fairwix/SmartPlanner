using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPlanner.Domain.Entities
{
    public class SecurityAuditLog : BaseEntity
    {
        public SecurityEventType EventType { get; set; }
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public bool Success { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public virtual User? User { get; set; }
    }

    public enum SecurityEventType
    {
        Login,
        FailedLogin,
        Logout,
        TokenRefresh,

        Register,
        EmailConfirmed,

        PasswordResetRequested,
        PasswordReset,
        PasswordChanged,

        UserCreated,
        UserUpdated,
        UserDeleted,
        UserBlocked,
        UserUnblocked,

        RoleAssigned,
        RoleRevoked,

        MultipleFailedLogins,
        AccessDenied,
        ExpiredTokenUsed,
        SuspiciousIpAddress,
        SuspiciousUserAgent
    }
}
