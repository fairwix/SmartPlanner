// Application/Security/Services/AuditLogCleanupService.cs (ПЕРЕМЕСТИТЕ в Application!)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Security.Services
{
    public class AuditLogCleanupService : BackgroundService
    {
        private readonly ILogger<AuditLogCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AuditLogCleanupService(
            ILogger<AuditLogCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Ждем 24 часа
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    // Удаляем логи старше 90 дней
                    var cutoffDate = DateTime.UtcNow.AddDays(-90);
                    var oldLogs = await context.SecurityAuditLogs
                        .Where(log => log.Timestamp < cutoffDate)
                        .ToListAsync(stoppingToken);

                    if (oldLogs.Any())
                    {
                        context.SecurityAuditLogs.RemoveRange(oldLogs);
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Cleaned up {Count} old security audit logs", oldLogs.Count);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error cleaning up audit logs");
                    // Ждем 1 час перед повторной попыткой
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }
}
