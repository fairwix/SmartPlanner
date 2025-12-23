using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.API.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditService auditService)
        {
            if (context.Request.Method == "OPTIONS" ||
                context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                if (context.Response.StatusCode >= 400)
                {
                    await LogFailedRequestAsync(context, auditService);
                }
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(context, ex, auditService);
                throw;
            }
            finally
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogFailedRequestAsync(HttpContext context, IAuditService auditService)
        {
            var userId = context.User?.FindFirst("userId")?.Value;
            Guid? userIdGuid = userId != null ? Guid.Parse(userId) : null;

            await auditService.LogSecurityEventAsync(
                SecurityEventType.AccessDenied,
                userIdGuid,
                ipAddress: context.Connection.RemoteIpAddress?.ToString(),
                userAgent: context.Request.Headers.UserAgent,
                success: false,
                details: new
                {
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    StatusCode = context.Response.StatusCode,
                    Query = context.Request.QueryString.Value
                });
        }

        private async Task LogExceptionAsync(HttpContext context, Exception ex, IAuditService auditService)
        {
            var userId = context.User?.FindFirst("userId")?.Value;
            Guid? userIdGuid = userId != null ? Guid.Parse(userId) : null;

            await auditService.LogSecurityEventAsync(
                SecurityEventType.AccessDenied,
                userIdGuid,
                ipAddress: context.Connection.RemoteIpAddress?.ToString(),
                userAgent: context.Request.Headers.UserAgent,
                success: false,
                details: new
                {
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    Exception = ex.Message,
                    StackTrace = ex.StackTrace
                });
        }
    }
}
