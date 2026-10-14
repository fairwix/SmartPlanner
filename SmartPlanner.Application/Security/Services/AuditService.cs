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
        private readonly IServiceScopeFactory _serviceScopeFactory; // ← Измените на IServiceScopeFactory

        public AuditService(
            IApplicationDbContext context,
            ILogger<AuditService> logger,
            IServiceScopeFactory serviceScopeFactory) // ← Измените конструктор
        {
            _context = context;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
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
                var auditLog = new SecurityAuditLog
                {
                    Id = Guid.NewGuid(), // ← Важно: генерируем ID сразу
                    EventType = eventType,
                    UserId = userId,
                    Email = email,
                    IpAddress = ipAddress ?? string.Empty,
                    UserAgent = userAgent,
                    Success = success,
                    Timestamp = DateTime.UtcNow,
                    Details = details != null ? JsonSerializer.Serialize(details) : null
                };

                // Сохраняем синхронно в текущем контексте (основная запись)
                await _context.SecurityAuditLogs.AddAsync(auditLog, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Security event logged: {EventType} for user {UserId}",
                    eventType, userId);

                // Если нужно дополнительное фоновое логирование
                if (!success && eventType == SecurityEventType.FailedLogin)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Для фоновой задачи создаем новый scope
                            using var scope = _serviceScopeFactory.CreateScope();
                            var scopedContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                            // Можете выполнить дополнительные проверки здесь
                            await CheckForSuspiciousActivityInBackgroundAsync(
                                scopedContext, ipAddress, userId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Background security check failed");
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event {EventType}", eventType);
                // Здесь можно добавить fallback-логирование (например, в файл)
            }
        }

        private async Task CheckForSuspiciousActivityInBackgroundAsync(
            IApplicationDbContext context,
            string? ipAddress,
            Guid? userId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return;

            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            var failedLoginCount = await context.SecurityAuditLogs
                .CountAsync(log =>
                    log.EventType == SecurityEventType.FailedLogin &&
                    log.IpAddress == ipAddress &&
                    log.Timestamp >= fiveMinutesAgo,
                    cancellationToken);

            if (failedLoginCount >= 5)
            {
                var detailsLog = new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = SecurityEventType.MultipleFailedLogins,
                    UserId = userId,
                    IpAddress = ipAddress,
                    Success = false,
                    Timestamp = DateTime.UtcNow,
                    Details = JsonSerializer.Serialize(new
                    {
                        FailedAttempts = failedLoginCount,
                        TimeWindow = "5 minutes"
                    })
                };

                await context.SecurityAuditLogs.AddAsync(detailsLog, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
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
            var failedLoginCount = await GetFailedLoginCountAsync(ipAddress, fiveMinutesAgo, cancellationToken);

            if (failedLoginCount >= 5)
            {
                await LogSecurityEventAsync(
                    eventType: SecurityEventType.MultipleFailedLogins,
                    userId: userId,
                    ipAddress: ipAddress,
                    success: false,
                    details: new { FailedAttempts = failedLoginCount, TimeWindow = "5 minutes" },
                    cancellationToken: cancellationToken);

                return true;
            }

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
