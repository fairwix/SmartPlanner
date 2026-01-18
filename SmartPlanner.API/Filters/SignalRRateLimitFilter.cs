using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace SmartPlanner.API.Filters
{
    public class SignalRRateLimitFilter : IHubFilter
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SignalRRateLimitFilter> _logger;

        public SignalRRateLimitFilter(IMemoryCache cache, ILogger<SignalRRateLimitFilter> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            var userId = invocationContext.Context.UserIdentifier ?? "anonymous";
            var hubName = invocationContext.Hub.GetType().Name;
            var methodName = invocationContext.HubMethodName;

            if (methodName == "SendTestNotification")
            {
                return await next(invocationContext);
            }

            // Ключ для rate limit
            var key = $"signalr:method:{hubName}:{methodName}:{userId}";

            // НАСТРОЙКА ЛИМИТОВ ДЛЯ КОНКРЕТНЫХ МЕТОДОВ
            var (limit, windowSeconds) = methodName switch
            {
                // Строгий лимит для тестовых уведомлений
                "SendTestNotification" => (5, 60),     // 5 раз в минуту

                // Для остальных методов
                _ => (20, 10)                         // 20 раз в 10 секунд
            };

            // Проверяем rate limit
            if (!CheckRateLimit(key, limit, windowSeconds))
            {
                _logger.LogWarning($"Rate limit exceeded for {userId} on {hubName}.{methodName}");
                throw new HubException("Слишком много запросов. Подождите немного.");
            }

            return await next(invocationContext);
        }

        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            var userId = context.Context.UserIdentifier ?? context.Context.ConnectionId;
            var ipAddress = context.Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Лимит на подключения с одного IP
            var ipKey = $"signalr:connect:ip:{ipAddress}";
            if (!CheckRateLimit(ipKey, limit: 10, windowSeconds: 60))
            {
                _logger.LogWarning($"Connection rate limit exceeded for IP {ipAddress}");
                context.Context.Abort();
                return;
            }

            // Лимит на подключения пользователя
            var userKey = $"signalr:connect:user:{userId}";
            if (!CheckRateLimit(userKey, limit: 5, windowSeconds: 60))
            {
                _logger.LogWarning($"Connection rate limit exceeded for user {userId}");
                context.Context.Abort();
                return;
            }

            await next(context);
        }

        private bool CheckRateLimit(string key, int limit, int windowSeconds)
        {
            if (!_cache.TryGetValue(key, out RateLimitInfo info))
            {
                info = new RateLimitInfo
                {
                    Count = 1,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(windowSeconds)
                };
                _cache.Set(key, info, info.ExpiresAt);
                return true;
            }

            if (DateTime.UtcNow > info.ExpiresAt)
            {
                info.Count = 1;
                info.ExpiresAt = DateTime.UtcNow.AddSeconds(windowSeconds);
                _cache.Set(key, info, info.ExpiresAt);
                return true;
            }

            if (info.Count >= limit)
            {
                return false;
            }

            info.Count++;
            _cache.Set(key, info, info.ExpiresAt);
            return true;
        }

        private class RateLimitInfo
        {
            public int Count { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
