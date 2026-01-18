// SmartPlanner.API/Hubs/FileHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Collections.Concurrent;

namespace SmartPlanner.API.Hubs
{
    /// <summary>
    /// Hub для real-time операций с файлами
    /// </summary>
    //[Authorize]
    public class FileHub : Hub
    {
        private readonly ILogger<FileHub> _logger;

        // Храним прогресс загрузки файлов (UploadId -> Progress)
        private static readonly ConcurrentDictionary<string, FileUploadProgress> _uploadProgress = new();

        // Храним активные загрузки пользователя (UserId -> List<UploadId>)
        private static readonly ConcurrentDictionary<string, List<string>> _userUploads = new();

        public FileHub(ILogger<FileHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
/// Вызывается при подключении клиента
/// </summary>
public override async Task OnConnectedAsync()
{
    var userId = GetUserId();
    var connectionId = Context.ConnectionId;

    if (string.IsNullOrEmpty(userId) || userId == "anonymous")
    {
        _logger.LogWarning($"🚫 Отклонено подключение: невалидный userId");

        // 🔥 ДЛЯ POSTMAN: Отправляем ошибку перед закрытием
        await Clients.Caller.SendAsync("Error", new
        {
            Message = "Неавторизованный доступ",
            Code = "UNAUTHORIZED"
        });

        // Ждем немного чтобы сообщение успело отправиться
        await Task.Delay(100);

        Context.Abort();
        return;
    }

    // Добавляем в группу пользователя для файловых операций
    await Groups.AddToGroupAsync(connectionId, $"files_user_{userId}");

    var ipAddress = Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
    _logger.LogInformation(
        $"🔐 Подключение: User={userId}, Connection={connectionId}, IP={ipAddress}");

    _logger.LogInformation(
        "📁 Пользователь {UserId} подключился к FileHub. ConnectionId: {ConnectionId}",
        userId, connectionId);

    // 🔥 ДЛЯ POSTMAN: Отправляем JSON в правильном формате
    var connectionInfo = new
    {
        Message = "✅ Подключение к файловому хабу установлено",
        ConnectionId = connectionId,
        UserId = userId,
        Timestamp = DateTime.UtcNow,
        Type = "ConnectionEstablished"
    };

    // Отправляем как обычное сообщение
    await Clients.Caller.SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(connectionInfo));

    // И как структурированное сообщение
    await Clients.Caller.SendAsync("FileHubConnected", connectionInfo);

    // Отправляем активные загрузки пользователя
    await SendActiveUploads(userId);

    await base.OnConnectedAsync();
}

/// <summary>
/// Запускает keep-alive сообщения для поддержания соединения
/// </summary>
private async Task StartKeepAlive(string connectionId, string userId)
{
    try
    {
        // Ждем 5 секунд после подключения
        await Task.Delay(5000);

        while (true)
        {
            // Проверяем что соединение еще живо
            if (string.IsNullOrEmpty(connectionId)) break;

            // Отправляем ping
            await Clients.Client(connectionId).SendAsync("KeepAlive", new
            {
                Type = "Ping",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            });

            // Ждем 10 секунд до следующего ping
            await Task.Delay(10000);
        }
    }
    catch
    {
        // Игнорируем ошибки - соединение закрыто
    }
}
        /// <summary>
        /// Вызывается при отключении клиента
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    $"⚠️ Отключение с ошибкой: User={userId}, Connection={connectionId}");
            }
            else
            {
                _logger.LogInformation(
                    $"🔐 Отключение: User={userId}, Connection={connectionId}");
            }

            await base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Начать отслеживание загрузки файла
        /// </summary>
        public async Task StartTrackingUpload(string uploadId, string fileName, long fileSize, int totalChunks)
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            var progress = new FileUploadProgress
            {
                UploadId = uploadId,
                UserId = userId,
                FileName = fileName,
                FileSize = fileSize,
                TotalChunks = totalChunks,
                UploadedChunks = 0,
                StartedAt = DateTime.UtcNow,
                ConnectionId = connectionId,
                Status = "uploading"
            };

            _uploadProgress[uploadId] = progress;

            // Добавляем в список загрузок пользователя
            _userUploads.AddOrUpdate(
                userId,
                new List<string> { uploadId },
                (key, existingList) =>
                {
                    if (!existingList.Contains(uploadId))
                        existingList.Add(uploadId);
                    return existingList;
                });

            _logger.LogInformation(
                "🚀 Начата отслеживание загрузки {UploadId}. Файл: {FileName}, Размер: {FileSize}, Чанков: {TotalChunks}, Пользователь: {UserId}",
                uploadId, fileName, fileSize, totalChunks, userId);

            await Clients.Group($"files_user_{userId}").SendAsync("UploadStarted", new
            {
                UploadId = uploadId,
                FileName = fileName,
                FileSize = fileSize,
                TotalChunks = totalChunks,
                StartedAt = progress.StartedAt,
                UserId = userId
            });

            await Clients.Caller.SendAsync("TrackingStarted", new
            {
                UploadId = uploadId,
                Message = "Отслеживание загрузки начато",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Обновить прогресс загрузки файла
        /// </summary>
        public async Task UpdateUploadProgress(string uploadId, int uploadedChunks, string? status = null)
        {
            if (_uploadProgress.TryGetValue(uploadId, out var progress))
            {
                var userId = GetUserId();

                // Проверяем права - только владелец может обновлять прогресс
                if (progress.UserId != userId)
                {
                    await Clients.Caller.SendAsync("Error", new
                    {
                        Message = "Нет прав для обновления этого прогресса",
                        UploadId = uploadId
                    });
                    return;
                }

                progress.UploadedChunks = uploadedChunks;
                progress.Progress = (double)uploadedChunks / progress.TotalChunks * 100;
                progress.LastUpdate = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(status))
                {
                    progress.Status = status;
                }

                _logger.LogDebug(
                    "📊 Прогресс загрузки {UploadId}: {Uploaded}/{Total} ({Progress:0.00}%)",
                    uploadId, uploadedChunks, progress.TotalChunks, progress.Progress);

                // Отправляем обновление владельцу
                await Clients.Group($"files_user_{userId}").SendAsync("UploadProgressUpdated", new
                {
                    UploadId = uploadId,
                    UploadedChunks = uploadedChunks,
                    TotalChunks = progress.TotalChunks,
                    Progress = progress.Progress,
                    Status = progress.Status,
                    FileName = progress.FileName,
                    FileSize = progress.FileSize,
                    EstimatedTime = CalculateEstimatedTime(progress),
                    UpdatedAt = progress.LastUpdate
                });

                // Если загрузка завершена
                if (uploadedChunks >= progress.TotalChunks)
                {
                    progress.Status = "completed";
                    progress.CompletedAt = DateTime.UtcNow;

                    await Clients.Group($"files_user_{userId}").SendAsync("UploadCompleted", new
                    {
                        UploadId = uploadId,
                        FileName = progress.FileName,
                        FileSize = progress.FileSize,
                        TotalChunks = progress.TotalChunks,
                        StartedAt = progress.StartedAt,
                        CompletedAt = progress.CompletedAt,
                        Duration = ((progress.CompletedAt ?? DateTime.UtcNow) - progress.StartedAt).TotalSeconds,
                        UserId = userId
                    });

                    // Удаляем из активных загрузок через 1 минуту
                    _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ =>
                    {
                        _uploadProgress.TryRemove(uploadId, out FileUploadProgress _);
                    });
                }
            }
            else
            {
                await Clients.Caller.SendAsync("Error", new
                {
                    Message = "Загрузка не найдена",
                    UploadId = uploadId
                });
            }
        }

        /// <summary>
        /// Получить прогресс загрузки файла
        /// </summary>
        public async Task<object> GetUploadProgress(string uploadId)
        {
            if (_uploadProgress.TryGetValue(uploadId, out var progress))
            {
                var userId = GetUserId();

                // Проверяем права - только владелец может видеть прогресс
                if (progress.UserId != userId)
                {
                    return new
                    {
                        Error = "Нет прав для просмотра этого прогресса",
                        UploadId = uploadId
                    };
                }

                var response = new
                {
                    UploadId = progress.UploadId,
                    FileName = progress.FileName,
                    FileSize = progress.FileSize,
                    TotalChunks = progress.TotalChunks,
                    UploadedChunks = progress.UploadedChunks,
                    Progress = progress.Progress,
                    Status = progress.Status,
                    StartedAt = progress.StartedAt,
                    LastUpdate = progress.LastUpdate,
                    EstimatedTime = CalculateEstimatedTime(progress),
                    ConnectionId = progress.ConnectionId
                };

                await Clients.Caller.SendAsync("UploadProgressRetrieved", response);

                return response;
            }

            return new
            {
                Error = "Загрузка не найдена",
                UploadId = uploadId
            };
        }

        /// <summary>
        /// Уведомить о начале скачивания файла
        /// </summary>
        public async Task NotifyFileDownload(Guid fileId, string fileName)
        {
            var userId = GetUserId();

            _logger.LogInformation(
                "⬇️ Пользователь {UserId} начал скачивание файла {FileId}: {FileName}",
                userId, fileId, fileName);

            var downloadInfo = new
            {
                FileId = fileId,
                FileName = fileName,
                UserId = userId,
                UserName = Context.User?.Identity?.Name,
                StartedAt = DateTime.UtcNow,
                IsPublic = false // Можно получить из БД
            };

            // Отправляем уведомление владельцу файла (если это не он сам)
            // Здесь нужно получить ownerId из БД
            // await Clients.Group($"user_{ownerId}").SendAsync("FileDownloadStarted", downloadInfo);

            // Отправляем в группу файла для всех подписчиков
            await Clients.Group($"file_{fileId}").SendAsync("FileDownloadStarted", downloadInfo);

            await Clients.Caller.SendAsync("DownloadNotificationSent", new
            {
                FileId = fileId,
                FileName = fileName,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Уведомить о завершении загрузки файла
        /// </summary>
        public async Task NotifyFileUploadCompleted(Guid fileId, string fileName, long fileSize, bool isDuplicate = false)
        {
            var userId = GetUserId();

            _logger.LogInformation(
                "✅ Пользователь {UserId} загрузил файл {FileId}: {FileName}, Размер: {FileSize}, Дубликат: {IsDuplicate}",
                userId, fileId, fileName, fileSize, isDuplicate);

            var uploadInfo = new
            {
                FileId = fileId,
                FileName = fileName,
                FileSize = fileSize,
                UserId = userId,
                UserName = Context.User?.Identity?.Name,
                UploadedAt = DateTime.UtcNow,
                IsDuplicate = isDuplicate,
                Message = isDuplicate ? "Файл уже существовал в системе" : "Файл успешно загружен"
            };

            // Отправляем в группу пользователя
            await Clients.Group($"files_user_{userId}").SendAsync("FileUploadCompleted", uploadInfo);

            // Если файл публичный - отправляем всем подписчикам
            // await Clients.Group($"file_{fileId}").SendAsync("NewFileAvailable", uploadInfo);

            await Clients.Caller.SendAsync("UploadCompletedNotificationSent", uploadInfo);
        }

        /// <summary>
        /// Подписаться на события файла
        /// </summary>
        public async Task SubscribeToFileEvents(Guid fileId)
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            await Groups.AddToGroupAsync(connectionId, $"file_{fileId}");

            _logger.LogInformation(
                "🔔 Пользователь {UserId} подписался на события файла {FileId}",
                userId, fileId);

            await Clients.Caller.SendAsync("SubscribedToFileEvents", new
            {
                FileId = fileId,
                Message = "✅ Подписан на события файла",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Получить активные загрузки пользователя
        /// </summary>
        public async Task<object> GetActiveUploads()
        {
            var userId = GetUserId();

            var activeUploads = new List<object>();

            if (_userUploads.TryGetValue(userId, out var uploadIds))
            {
                foreach (var uploadId in uploadIds)
                {
                    if (_uploadProgress.TryGetValue(uploadId, out var progress))
                    {
                        activeUploads.Add(new
                        {
                            UploadId = progress.UploadId,
                            FileName = progress.FileName,
                            FileSize = progress.FileSize,
                            Progress = progress.Progress,
                            Status = progress.Status,
                            StartedAt = progress.StartedAt,
                            EstimatedTime = CalculateEstimatedTime(progress)
                        });
                    }
                }
            }

            await Clients.Caller.SendAsync("ActiveUploadsList", new
            {
                Uploads = activeUploads,
                Count = activeUploads.Count,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });

            return new
            {
                Count = activeUploads.Count,
                Uploads = activeUploads
            };
        }

        /// <summary>
        /// Отменить загрузку файла
        /// </summary>
        public async Task CancelUpload(string uploadId)
        {
            if (_uploadProgress.TryGetValue(uploadId, out var progress))
            {
                var userId = GetUserId();

                if (progress.UserId != userId)
                {
                    await Clients.Caller.SendAsync("Error", new
                    {
                        Message = "Нет прав для отмены этой загрузки",
                        UploadId = uploadId
                    });
                    return;
                }

                progress.Status = "cancelled";
                progress.CancelledAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "❌ Пользователь {UserId} отменил загрузку {UploadId}: {FileName}",
                    userId, uploadId, progress.FileName);

                await Clients.Group($"files_user_{userId}").SendAsync("UploadCancelled", new
                {
                    UploadId = uploadId,
                    FileName = progress.FileName,
                    Progress = progress.Progress,
                    CancelledAt = progress.CancelledAt,
                    UserId = userId
                });

                // Удаляем из активных загрузок
                _uploadProgress.TryRemove(uploadId, out _);

                if (_userUploads.TryGetValue(userId, out var uploads))
                {
                    uploads.Remove(uploadId);
                }

                await Clients.Caller.SendAsync("UploadCancelledConfirmed", new
                {
                    UploadId = uploadId,
                    Message = "Загрузка отменена",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #region Вспомогательные методы

        /// <summary>
        /// Отправить активные загрузки пользователю
        /// </summary>
        private async Task SendActiveUploads(string userId)
        {
            if (_userUploads.TryGetValue(userId, out var uploadIds))
            {
                var activeUploads = uploadIds
                    .Where(id => _uploadProgress.TryGetValue(id, out var p) && p.Status == "uploading")
                    .Select(id => _uploadProgress[id])
                    .Select(p => new
                    {
                        p.UploadId,
                        p.FileName,
                        p.Progress,
                        p.Status
                    })
                    .ToList();

                if (activeUploads.Any())
                {
                    await Clients.Group($"files_user_{userId}").SendAsync("ActiveUploads", new
                    {
                        Uploads = activeUploads,
                        Count = activeUploads.Count,
                        UserId = userId
                    });
                }
            }
        }

        /// <summary>
        /// Очистить неактивные загрузки пользователя
        /// </summary>
        private void CleanupUserUploads(string userId)
        {
            if (_userUploads.TryGetValue(userId, out var uploadIds))
            {
                // Удаляем завершенные/отмененные загрузки
                var completedUploads = uploadIds
                    .Where(id => _uploadProgress.TryGetValue(id, out var p) &&
                                (p.Status == "completed" || p.Status == "cancelled" ||
                                 (DateTime.UtcNow - p.LastUpdate).TotalMinutes > 30))
                    .ToList();

                foreach (var uploadId in completedUploads)
                {
                    uploadIds.Remove(uploadId);
                    _uploadProgress.TryRemove(uploadId, out _);
                }

                if (uploadIds.Count == 0)
                {
                    _userUploads.TryRemove(userId, out _);
                }
            }
        }

        /// <summary>
        /// Рассчитать оставшееся время загрузки
        /// </summary>
        private string CalculateEstimatedTime(FileUploadProgress progress)
        {
            if (progress.Progress <= 0 || progress.Progress >= 100)
                return "00:00:00";

            var elapsed = DateTime.UtcNow - progress.StartedAt;
            var totalEstimated = TimeSpan.FromSeconds(elapsed.TotalSeconds / (progress.Progress / 100));
            var remaining = totalEstimated - elapsed;

            return remaining.ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// Получить ID текущего пользователя из claims
        /// </summary>
        private string GetUserId()
        {
            return Context.User?.FindFirst("userId")?.Value
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value
                ?? "anonymous";
        }

        #endregion

        #region Вспомогательные классы

        private class FileUploadProgress
        {
            public string UploadId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public int TotalChunks { get; set; }
            public int UploadedChunks { get; set; }
            public double Progress { get; set; }
            public string Status { get; set; } = "uploading"; // uploading, completed, cancelled, error
            public DateTime StartedAt { get; set; }
            public DateTime LastUpdate { get; set; }
            public DateTime? CompletedAt { get; set; }
            public DateTime? CancelledAt { get; set; }
            public string ConnectionId { get; set; } = string.Empty;
        }

        #endregion
    }
}
