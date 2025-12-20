using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace SmartPlanner.API.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
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
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var key = $"{_keyPrefix}:{ipAddress}";
            if (cache == null)
            {
                throw new InvalidOperationException("IMemoryCache is not registered in the service container");
            }

            if (!cache.TryGetValue(key, out int requestCount))
            {
                requestCount = 0;
            }

            if (requestCount >= _limit)
            {
                context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
                {
                    StatusCode = 429,
                    Message = $"Rate limit exceeded. Try again in {_seconds} seconds."
                })
                {
                    StatusCode = 429
                };
                return;
            }

            requestCount++;
            cache.Set(key, requestCount, TimeSpan.FromSeconds(_seconds));

            base.OnActionExecuting(context);
        }
    }
}
