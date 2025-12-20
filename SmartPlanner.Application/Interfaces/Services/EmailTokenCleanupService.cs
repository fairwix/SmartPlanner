// Infrastructure/Services/EmailTokenCleanupService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Services
{
    public class EmailTokenCleanupService : BackgroundService
    {
        private readonly ILogger<EmailTokenCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

        public EmailTokenCleanupService(
            ILogger<EmailTokenCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Token Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    var now = DateTime.UtcNow;

                    // Очистка истёкших email confirmation tokens
                    var expiredEmailTokens = await context.EmailConfirmationTokens
                        .Where(t => t.ExpiresAt < now || t.IsUsed)
                        .ToListAsync(stoppingToken);

                    if (expiredEmailTokens.Any())
                    {
                        context.EmailConfirmationTokens.RemoveRange(expiredEmailTokens);
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Cleaned up {Count} expired email confirmation tokens", expiredEmailTokens.Count);
                    }

                    // Очистка истёкших password reset tokens
                    var expiredPasswordTokens = await context.PasswordResetTokens
                        .Where(t => t.ExpiresAt < now || t.IsUsed)
                        .ToListAsync(stoppingToken);

                    if (expiredPasswordTokens.Any())
                    {
                        context.PasswordResetTokens.RemoveRange(expiredPasswordTokens);
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Cleaned up {Count} expired password reset tokens", expiredPasswordTokens.Count);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error cleaning up email tokens");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Email Token Cleanup Service stopped");
        }
    }
}
