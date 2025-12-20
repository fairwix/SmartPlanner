using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SmartPlanner.Application.Security.Services
{
    public interface IAuditService
    {
        Task LogSecurityEventAsync(
            SecurityEventType eventType,
            Guid? userId = null,
            string? email = null,
            string? ipAddress = null,
            string? userAgent = null,
            bool success = true,
            object? details = null,
            CancellationToken cancellationToken = default);

        Task<int> GetFailedLoginCountAsync(string ipAddress, DateTime since, CancellationToken cancellationToken = default);
        Task<bool> CheckSuspiciousActivityAsync(string ipAddress, Guid? userId, CancellationToken cancellationToken = default);
    }

    public class AuditService : IAuditService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;
        private readonly IServiceProvider _serviceProvider; // Для фоновых задач

        public AuditService(
            IApplicationDbContext context,
            ILogger<AuditService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task LogSecurityEventAsync(
            SecurityEventType eventType,
            Guid? userId = null,
            string? email = null,
            string? ipAddress = null,
            string? userAgent = null,
            bool success = true,
            object? details = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Создаем запись лога
                var auditLog = new SecurityAuditLog
                {
                    EventType = eventType,
                    UserId = userId,
                    Email = email,
                    IpAddress = ipAddress ?? string.Empty,
                    UserAgent = userAgent,
                    Success = success,
                    Timestamp = DateTime.UtcNow,
                    Details = details != null ? JsonSerializer.Serialize(details) : null
                };

                // Асинхронное сохранение (не блокируем основной поток)
                // Используем фоновую задачу как в книге, глава 17
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var scopedContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                        await scopedContext.SecurityAuditLogs.AddAsync(auditLog, cancellationToken);
                        await scopedContext.SaveChangesAsync(cancellationToken);

                        _logger.LogDebug("Security event logged: {EventType} for user {UserId}",
                            eventType, userId);
                    }
                    catch (Exception ex)
                    {
                        // Не прерываем основной поток из-за ошибки логирования
                        _logger.LogError(ex, "Failed to save security audit log for event {EventType}",
                            eventType);
                    }
                }, cancellationToken);

                // Дополнительная логика для подозрительной активности
                if (!success && eventType == SecurityEventType.FailedLogin)
                {
                    await CheckForSuspiciousActivityAsync(ipAddress, userId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event {EventType}", eventType);
                // НЕ пробрасываем исключение - логирование не должно ломать основной поток
            }
        }

        public async Task<int> GetFailedLoginCountAsync(string ipAddress, DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.SecurityAuditLogs
                .CountAsync(log =>
                    log.EventType == SecurityEventType.FailedLogin &&
                    log.IpAddress == ipAddress &&
                    log.Timestamp >= since,
                    cancellationToken);
        }

        public async Task<bool> CheckSuspiciousActivityAsync(string ipAddress, Guid? userId, CancellationToken cancellationToken = default)
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            // Множественные неудачные попытки входа
            var failedLoginCount = await GetFailedLoginCountAsync(ipAddress, fiveMinutesAgo, cancellationToken);

            if (failedLoginCount >= 5)
            {
                await LogSecurityEventAsync(
                    SecurityEventType.MultipleFailedLogins,
                    userId,
                    ipAddress: ipAddress,
                    success: false,
                    details: new { FailedAttempts = failedLoginCount, TimeWindow = "5 minutes" },
                    cancellationToken: cancellationToken);

                return true;
            }

            // Дополнительные проверки можно добавить здесь
            return false;
        }

        private async Task CheckForSuspiciousActivityAsync(string? ipAddress, Guid? userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return;

            await CheckSuspiciousActivityAsync(ipAddress, userId, cancellationToken);
        }
    }
}
