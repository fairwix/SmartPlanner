using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using Microsoft.AspNetCore.Mvc;

namespace SmartPlanner.API.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _limit;
        private readonly int _seconds;
        private readonly string _keyPrefix;

        public RateLimitAttribute(string keyPrefix, int limit = 5, int seconds = 60)
        {
            _limit = limit;
            _seconds = seconds;
            _keyPrefix = keyPrefix;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();

            // Для SignalR используем ConnectionId, для API - IP
            var identifier = context.HttpContext.Request.Path.StartsWithSegments("/hubs")
                ? context.HttpContext.Connection.Id
                : context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var key = $"{_keyPrefix}:{identifier}";

            if (cache == null)
            {
                throw new InvalidOperationException("IMemoryCache is not registered");
            }

            if (!cache.TryGetValue(key, out int requestCount))
            {
                requestCount = 0;
            }

            if (requestCount >= _limit)
            {
                context.Result = new ObjectResult(new
                {
                    StatusCode = 429,
                    Message = $"Rate limit exceeded. Try again in {_seconds} seconds.",
                    RetryAfter = _seconds
                })
                {
                    StatusCode = 429
                };

                // Добавляем заголовок Retry-After
                context.HttpContext.Response.Headers["Retry-After"] = _seconds.ToString();
                return;
            }

            requestCount++;
            cache.Set(key, requestCount, TimeSpan.FromSeconds(_seconds));

            base.OnActionExecuting(context);
        }
    }
}
