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
        var responseStream = new CountingWriteStream();
        var context = new DefaultHttpContext();
        context.Response.Body = responseStream;
        var middleware = new AuditLoggingMiddleware(async httpContext =>
        {
            Assert.Same(responseStream, httpContext.Response.Body);
            var chunk = new byte[64 * 1024];
            for (var index = 0; index < 1024; index++)
                await httpContext.Response.Body.WriteAsync(chunk);
        }, NullLogger<AuditLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context, Mock.Of<IAuditService>());

        Assert.Equal(64L * 1024 * 1024, responseStream.BytesWritten);
    }

    private sealed class CountingWriteStream : Stream
    {
        public long BytesWritten { get; private set; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => BytesWritten;
        public override long Position { get => BytesWritten; set => throw new NotSupportedException(); }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => BytesWritten += count;
        public override void Write(ReadOnlySpan<byte> buffer) => BytesWritten += buffer.Length;
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            BytesWritten += buffer.Length;
            return ValueTask.CompletedTask;
        }
    }
}
