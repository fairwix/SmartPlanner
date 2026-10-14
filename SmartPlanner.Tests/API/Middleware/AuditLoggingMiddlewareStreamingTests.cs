using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartPlanner.API.Middleware;
using SmartPlanner.Application.Security.Services;
using Xunit;

namespace SmartPlanner.Tests.API.Middleware;

public sealed class AuditLoggingMiddlewareStreamingTests
{
    [Fact]
    public async Task InvokeAsync_PassesOriginalResponseStreamThroughWithoutBuffering()
    {
        var responseStream = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = responseStream;
        var middleware = new AuditLoggingMiddleware(async httpContext =>
        {
            Assert.Same(responseStream, httpContext.Response.Body);
            await httpContext.Response.Body.WriteAsync(new byte[1024 * 1024]);
        }, NullLogger<AuditLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context, Mock.Of<IAuditService>());

        Assert.Equal(1024 * 1024, responseStream.Length);
    }
}
