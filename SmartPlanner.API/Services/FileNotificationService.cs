using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SmartPlanner.API.Hubs;
using Microsoft.Extensions.Logging;

namespace SmartPlanner.API.Services
{
    public class FileNotificationService : IFileNotificationService
    {
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IHubContext<FileHub> _fileHub;
        private readonly ILogger<FileNotificationService> _logger;
        private readonly ConcurrentDictionary<string, (int count, DateTime window)> _rateLimits = new();
        private readonly TimeSpan _rateLimitWindow = TimeSpan.FromSeconds(10);
        private readonly int _maxNotificationsPerWindow = 5;

        public FileNotificationService(
            IHubContext<NotificationHub> notificationHub,
            IHubContext<FileHub> fileHub,
            ILogger<FileNotificationService> logger)
        {
            _notificationHub = notificationHub;
            _fileHub = fileHub;
            _logger = logger;
        }

        public async Task NotifyFileUploadedAsync(string userId, string fileName,
            long fileSize, Guid fileId, bool isDuplicate = false)
        {
            if (!CanSendNotification(userId))
            {
                _logger.LogWarning($"🚫 Rate limit exceeded for user {userId}");
                return;
            }
            try
            {
                _logger.LogInformation($"📤 Отправка уведомления о загрузке файла для пользователя {userId}");

                var notification = new
                {
                    Id = Guid.NewGuid(),
                    Title = "Файл загружен",
                    Message = $"Файл '{fileName}' успешно загружен",
                    Type = "success",
                    FileId = fileId,
                    FileName = fileName,
                    FileSize = fileSize,
                    IsDuplicate = isDuplicate,
                    CreatedAt = DateTime.UtcNow
                };

                // Отправляем через NotificationHub
                await _notificationHub.Clients.Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogInformation($"✅ Уведомление отправлено пользователю {userId}");

                UpdateRateLimit(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка отправки уведомления");
            }
        }

        private bool CanSendNotification(string userId)
        {
            var now = DateTime.UtcNow;
            var key = $"notify_{userId}";

            if (_rateLimits.TryGetValue(key, out var limit))
            {
                // Если окно времени истекло, сбрасываем счетчик
                if (now - limit.window > _rateLimitWindow)
                {
                    _rateLimits[key] = (1, now);
                    return true;
                }

                // Проверяем не превышен ли лимит
                if (limit.count >= _maxNotificationsPerWindow)
                {
                    return false;
                }

                // Увеличиваем счетчик
                _rateLimits[key] = (limit.count + 1, limit.window);
                return true;
            }

            // Первое уведомление в окне
            _rateLimits[key] = (1, now);
            return true;
        }

        private void UpdateRateLimit(string userId)
        {
            var key = $"notify_{userId}";
            var now = DateTime.UtcNow;

            if (_rateLimits.TryGetValue(key, out var limit))
            {
                _rateLimits[key] = (limit.count + 1, limit.window);
            }
        }

        public async Task NotifyFileDeletedAsync(string userId, string fileName,
            Guid fileId, string reason = "")
        {
            try
            {
                _logger.LogInformation(
                    "📤 Отправка уведомления об удалении файла. User: {UserId}, File: {FileName}, ID: {FileId}",
                    userId, fileName, fileId);

                // 1. Отправляем через NotificationHub
                await _notificationHub.Clients.Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        Id = Guid.NewGuid(),
                        Title = "Файл удален",
                        Message = string.IsNullOrEmpty(reason)
                            ? $"Файл '{fileName}' был удален"
                            : $"Файл '{fileName}' был удален: {reason}",
                        Type = "warning",
                        FileId = fileId,
                        FileName = fileName,
                        Reason = reason,
                        CreatedAt = DateTime.UtcNow,
                        Icon = "🗑️",
                        CanRestore = true
                    });

                // 2. Отправляем через FileHub
                await _fileHub.Clients.Group($"files_user_{userId}")
                    .SendAsync("FileDeleted", new
                    {
                        FileId = fileId,
                        FileName = fileName,
                        UserId = userId,
                        Reason = reason,
                        DeletedAt = DateTime.UtcNow
                    });

                _logger.LogInformation("✅ Уведомления об удалении файла отправлены");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при отправке уведомления об удалении файла");
            }
        }

        public async Task NotifyUploadProgressAsync(string userId, string uploadId, int progress)
        {
            try
            {
                await _fileHub.Clients.Group($"files_user_{userId}")
                    .SendAsync("UploadProgressUpdated", new
                    {
                        UploadId = uploadId,
                        Progress = progress,
                        UserId = userId,
                        UpdatedAt = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Не удалось отправить прогресс загрузки");
            }
        }

        public async Task NotifyUploadStartedAsync(string userId, string uploadId,
            string fileName, long fileSize)
        {
            try
            {
                await _fileHub.Clients.Group($"files_user_{userId}")
                    .SendAsync("UploadStarted", new
                    {
                        UploadId = uploadId,
                        FileName = fileName,
                        FileSize = fileSize,
                        UserId = userId,
                        StartedAt = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Не удалось отправить уведомление о начале загрузки");
            }
        }

        public async Task NotifyUploadCompletedAsync(string userId, string uploadId,
            string fileName, bool success)
        {
            try
            {
                await _fileHub.Clients.Group($"files_user_{userId}")
                    .SendAsync("UploadCompleted", new
                    {
                        UploadId = uploadId,
                        FileName = fileName,
                        Success = success,
                        UserId = userId,
                        CompletedAt = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Не удалось отправить уведомление о завершении загрузки");
            }
        }

        public async Task NotifyFileDownloadedAsync(string userId, string fileName, Guid fileId)
        {
            try
            {
                await _notificationHub.Clients.Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        Id = Guid.NewGuid(),
                        Title = "Файл скачан",
                        Message = $"Вы скачали файл '{fileName}'",
                        Type = "info",
                        FileId = fileId,
                        FileName = fileName,
                        CreatedAt = DateTime.UtcNow,
                        Icon = "⬇️"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Не удалось отправить уведомление о скачивании файла");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
