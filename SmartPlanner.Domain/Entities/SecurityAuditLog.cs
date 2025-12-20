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
        public string? Details { get; set; } // JSON
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Навигационное свойство (опционально)
        public virtual User? User { get; set; }
    }

    public enum SecurityEventType
    {
        // Аутентификация
        Login,
        FailedLogin,
        Logout,
        TokenRefresh,

        // Регистрация
        Register,
        EmailConfirmed,

        // Управление паролями
        PasswordResetRequested,
        PasswordReset,
        PasswordChanged,

        // Управление пользователями
        UserCreated,
        UserUpdated,
        UserDeleted,
        UserBlocked,
        UserUnblocked,

        // Управление ролями
        RoleAssigned,
        RoleRevoked,

        // Подозрительная активность
        MultipleFailedLogins,
        AccessDenied,
        ExpiredTokenUsed,
        SuspiciousIpAddress,
        SuspiciousUserAgent
    }
}
