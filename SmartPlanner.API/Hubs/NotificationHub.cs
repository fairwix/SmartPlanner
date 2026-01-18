using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace SmartPlanner.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly IMemoryCache _cache;

        // Статические коллекции для отслеживания подключений (в production используйте Redis)
        private static readonly ConcurrentDictionary<string, DateTime> _userConnections = new();
        private static readonly ConcurrentDictionary<string, int> _userConnectionCounts = new();
        private static readonly SemaphoreSlim _globalConnectionSemaphore = new SemaphoreSlim(50, 50); // Глобальный лимит 50 подключений
        private static readonly DateTime _appStartTime = DateTime.UtcNow;

        public NotificationHub(ILogger<NotificationHub> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;
            var ipAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation($"🔄 Попытка подключения: User={userId}, IP={ipAddress}, Connection={connectionId}");

            // 1. Проверка глобального лимита подключений
            if (!await _globalConnectionSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                _logger.LogWarning($"🚫 Глобальный лимит подключений достигнут для {userId}");
                Context.Abort();
                return;
            }

            try
            {
                // 2. Проверка rate limit по IP
                if (!CheckRateLimit($"ip_connect_{ipAddress}", limit: 5, windowSeconds: 60))
                {
                    _logger.LogWarning($"🚫 IP {ipAddress} превысил лимит подключений");
                    Context.Abort();
                    return;
                }

                // 3. Проверка лимита подключений для пользователя
                var userConnectionCount = _userConnectionCounts.AddOrUpdate(userId, 1, (key, count) => count + 1);
                if (userConnectionCount > 5) // Макс 5 подключений на пользователя
                {
                    _logger.LogWarning($"🚫 User {userId} превысил лимит подключений ({userConnectionCount}/5)");
                    _userConnectionCounts.AddOrUpdate(userId, 0, (key, count) => Math.Max(0, count - 1));
                    Context.Abort();
                    return;
                }

                // 4. Сохраняем информацию о подключении
                var connectionKey = $"{userId}_{connectionId}";
                _userConnections[connectionKey] = DateTime.UtcNow;

                // 5. Добавляем в группу пользователя
                await Groups.AddToGroupAsync(connectionId, $"user_{userId}");

                // 6. Также добавляем в общую группу для админов
                if (Context.User?.IsInRole("Admin") == true)
                {
                    await Groups.AddToGroupAsync(connectionId, "admin_notifications");
                }

                _logger.LogInformation($"✅ Подключен: User={userId}, Connections={userConnectionCount}, IP={ipAddress}");

                // 7. Отправляем информацию о подключении
                await Clients.Caller.SendAsync("Connected", new
                {
                    Message = "Подключено к системе уведомлений",
                    UserId = userId,
                    ConnectionId = connectionId,
                    MaxConnectionsPerUser = 5,
                    YourConnections = userConnectionCount,
                    TotalConnections = _userConnections.Count,
                    ServerUptime = (DateTime.UtcNow - _appStartTime).TotalMinutes,
                    Timestamp = DateTime.UtcNow
                });

                // 8. Уведомляем админов о новом подключении (если нужно)
                if (Context.User?.IsInRole("Admin") == false) // Не админ
                {
                    await Clients.Group("admin_notifications").SendAsync("UserConnected", new
                    {
                        UserId = userId,
                        ConnectionId = connectionId,
                        IpAddress = ipAddress,
                        Timestamp = DateTime.UtcNow
                    });
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при подключении пользователя {UserId}", userId);
                _globalConnectionSemaphore.Release();
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;
            var connectionKey = $"{userId}_{connectionId}";

            // Уменьшаем счетчик подключений пользователя
            _userConnectionCounts.AddOrUpdate(userId, 0, (key, count) => Math.Max(0, count - 1));

            // Удаляем из списка подключений
            _userConnections.TryRemove(connectionKey, out _);

            // Удаляем из групп
            await Groups.RemoveFromGroupAsync(connectionId, $"user_{userId}");
            await Groups.RemoveFromGroupAsync(connectionId, "admin_notifications");

            _logger.LogInformation($"❌ Отключен: User={userId}, Connection={connectionId}, Осталось подключений: {_userConnections.Count}");

            // Всегда освобождаем семафор
            _globalConnectionSemaphore.Release();

            await base.OnDisconnectedAsync(exception);
        }

        #region Public Methods

        /// <summary>
        /// Простой ping для проверки связи
        /// </summary>
        public async Task<string> Ping()
        {
            var userId = GetUserId();

            // Rate limit на ping
            if (!CheckRateLimit($"ping_{userId}", limit: 10, windowSeconds: 30))
            {
                throw new HubException("Слишком много ping запросов. Подождите 30 секунд.");
            }

            return $"Pong from NotificationHub at {DateTime.UtcNow} (User: {userId})";
        }

        /// <summary>
/// Отправка тестового уведомления самому себе
/// </summary>
public async Task SendTestNotification()
{
    var userId = GetUserId();
    var connectionId = Context.ConnectionId;

    _logger.LogDebug($"🔄 SendTestNotification вызван пользователем {userId}");

    // ✅ 1. Создаем ключ для rate limit
    var rateLimitKey = $"test_notify_{userId}";

    // ✅ 2. Проверяем rate limit с помощью метода CheckRateLimit
    if (!CheckRateLimit(rateLimitKey, limit: 5, windowSeconds: 60))
    {
        _logger.LogWarning($"🚫 Rate limit превышен для пользователя {userId} на SendTestNotification");

        // ✅ 3. Отправляем ошибку как уведомление (клиент увидит её в UI)
        await Clients.Caller.SendAsync("ReceiveNotification", new
        {
            Id = Guid.NewGuid(),
            Title = "⚠️ Превышен лимит",
            Message = "Слишком много тестовых уведомлений. Подождите 1 минуту.",
            Type = "warning",
            UserId = userId,
            ConnectionId = connectionId,
            Timestamp = DateTime.UtcNow,
            RetryAfterSeconds = 60,
            IsRateLimitError = true
        });

        // Логируем в консоль для отладки
        Console.WriteLine($"=== RATE LIMIT HIT: User {userId} exceeded test notification limit ===");
        return;
    }

    // ✅ 4. Если rate limit не превышен - отправляем тестовое уведомление
    _logger.LogDebug($"✅ Rate limit проверка пройдена для {userId}");

    var notificationId = Guid.NewGuid();
    var timestamp = DateTime.UtcNow;

    await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
    {
        Id = notificationId,
        Title = "Тест от SignalR",
        Message = $"Это тестовое уведомление #{GetNotificationCount(userId)} для пользователя {userId}",
        Type = "info",
        UserId = userId,
        ConnectionId = connectionId,
        Timestamp = timestamp,
        ServerTime = timestamp.ToString("HH:mm:ss"),
        IsTestNotification = true
    });

    _logger.LogInformation($"✅ Тестовое уведомление {notificationId} отправлено пользователю {userId}");

    // Обновляем счетчик уведомлений для пользователя
    UpdateNotificationCount(userId);
}

// Вспомогательные методы для отслеживания количества уведомлений
private static readonly ConcurrentDictionary<string, int> _notificationCounts = new();

private int GetNotificationCount(string userId)
{
    return _notificationCounts.GetOrAdd(userId, 0);
}

private void UpdateNotificationCount(string userId)
{
    _notificationCounts.AddOrUpdate(userId, 1, (key, oldValue) => oldValue + 1);
}

        /// <summary>
        /// Получить информацию о текущем подключении
        /// </summary>
        public async Task<object> GetConnectionInfo()
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            return new
            {
                UserId = userId,
                ConnectionId = connectionId,
                IsAuthenticated = Context.User?.Identity?.IsAuthenticated ?? false,
                UserName = Context.User?.Identity?.Name,
                Groups = new[] { $"user_{userId}" },
                YourConnections = _userConnectionCounts.TryGetValue(userId, out var count) ? count : 0,
                TotalConnections = _userConnections.Count,
                ServerTime = DateTime.UtcNow,
                Uptime = (DateTime.UtcNow - _appStartTime).ToString(@"dd\.hh\:mm\:ss")
            };
        }

        /// <summary>
        /// Получить статистику хаба (только для админов)
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<object> GetHubStats()
        {
            var stats = new
            {
                TotalConnections = _userConnections.Count,
                UniqueUsers = _userConnectionCounts.Count,
                ActiveAdmins = _userConnections.Count(kvp =>
                    kvp.Key.StartsWith("admin_") || IsAdminUser(kvp.Key.Split('_')[0])),
                ConnectionsByUser = _userConnectionCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                OldestConnection = _userConnections.Values.OrderBy(v => v).FirstOrDefault(),
                GlobalSemaphoreCount = _globalConnectionSemaphore.CurrentCount,
                ServerUptime = DateTime.UtcNow - _appStartTime,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("HubStatsResponse", stats);
            return stats;
        }

        /// <summary>
        /// Подписаться на дополнительные группы уведомлений
        /// </summary>
        public async Task SubscribeToGroup(string groupName)
        {
            var userId = GetUserId();

            // Проверяем, что группа не запрещенная
            if (IsRestrictedGroup(groupName))
            {
                throw new HubException($"Группа {groupName} недоступна для подписки");
            }

            // Rate limit на подписки
            if (!CheckRateLimit($"subscribe_{userId}", limit: 10, windowSeconds: 60))
            {
                throw new HubException("Слишком много запросов на подписку. Подождите 1 минуту.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("Subscribed", new
            {
                Group = groupName,
                Message = $"Подписан на группу {groupName}",
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation($"✅ User {userId} подписался на группу {groupName}");
        }

        /// <summary>
        /// Отписаться от группы
        /// </summary>
        public async Task UnsubscribeFromGroup(string groupName)
        {
            var userId = GetUserId();

            // Нельзя отписаться от основной группы пользователя
            if (groupName == $"user_{userId}")
            {
                throw new HubException("Нельзя отписаться от основной группы уведомлений");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            await Clients.Caller.SendAsync("Unsubscribed", new
            {
                Group = groupName,
                Message = $"Отписан от группы {groupName}",
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation($"✅ User {userId} отписался от группы {groupName}");
        }

        #endregion

        #region Private Methods

        private string GetUserId()
        {
            return Context.User?.FindFirst("userId")?.Value
                ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Требуется авторизация");
        }

        private bool CheckRateLimit(string key, int limit, int windowSeconds)
        {
            var cacheKey = $"ratelimit:{key}";

            if (!_cache.TryGetValue(cacheKey, out RateLimitInfo info))
            {
                info = new RateLimitInfo
                {
                    Count = 1,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(windowSeconds)
                };
                _cache.Set(cacheKey, info, info.ExpiresAt);
                return true;
            }

            if (DateTime.UtcNow > info.ExpiresAt)
            {
                info.Count = 1;
                info.ExpiresAt = DateTime.UtcNow.AddSeconds(windowSeconds);
                _cache.Set(cacheKey, info, info.ExpiresAt);
                return true;
            }

            if (info.Count >= limit)
            {
                return false;
            }

            info.Count++;
            _cache.Set(cacheKey, info, info.ExpiresAt);
            return true;
        }

        private bool IsAdminUser(string userId)
        {
            // Здесь можно добавить логику проверки ролей из базы данных
            // Пока используем простую проверку по claims
            return Context.User?.IsInRole("Admin") == true;
        }

        private bool IsRestrictedGroup(string groupName)
        {
            var restrictedGroups = new[]
            {
                "admin_notifications",
                "system",
                "internal"
            };

            return restrictedGroups.Contains(groupName) || groupName.StartsWith("admin_");
        }

        #endregion

        #region Helper Classes

        private class RateLimitInfo
        {
            public int Count { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        #endregion
    }
}
