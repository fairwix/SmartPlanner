// SmartPlanner.API/Middleware/CorsLoggingMiddleware.cs
using Microsoft.Extensions.Logging;

namespace SmartPlanner.API.Middleware;

public class CorsLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorsLoggingMiddleware> _logger;

    public CorsLoggingMiddleware(RequestDelegate next, ILogger<CorsLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        var method = context.Request.Method;
        var path = context.Request.Path;

        // Логируем только CORS запросы с Origin header
        if (!string.IsNullOrEmpty(origin))
        {
            var isPreflight = method == "OPTIONS" &&
                              context.Request.Headers.ContainsKey("Access-Control-Request-Method");

            if (isPreflight)
            {
                _logger.LogInformation("CORS Preflight: {Method} {Path} from Origin: {Origin}",
                    method, path, origin);
            }
            else
            {
                _logger.LogInformation("CORS Request: {Method} {Path} from Origin: {Origin}",
                    method, path, origin);
            }
        }

        await _next(context);
    }
}
