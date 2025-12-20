// SmartPlanner.Tests/API/Middleware/GlobalExceptionHandlingMiddlewareTests.cs
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using FluentValidation;
using SmartPlanner.API.Middleware;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SmartPlanner.Tests.API.Middleware
{
    public class GlobalExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<SmartPlanner.API.Middleware.GlobalExceptionHandlingMiddleware>> _mockLogger;

        public GlobalExceptionHandlingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<SmartPlanner.API.Middleware.GlobalExceptionHandlingMiddleware>>();
        }

        private DefaultHttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/api/test";
            return context;
        }

        [Fact]
        public async Task InvokeAsync_ValidationException_Returns400BadRequest()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SmartPlanner.API.Middleware.GlobalExceptionHandlingMiddleware(
                next: (innerContext) => throw new ValidationException("Validation failed"),
                logger: _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            Assert.Contains("Validation error", responseBody);
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Returns400BadRequest()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SmartPlanner.API.Middleware.GlobalExceptionHandlingMiddleware(
                next: (innerContext) => throw new ArgumentException("Invalid argument"),
                logger: _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Returns500InternalServerError()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = new SmartPlanner.API.Middleware.GlobalExceptionHandlingMiddleware(
                next: (innerContext) => throw new Exception("Something went wrong"),
                logger: _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_NoException_ContinuesPipeline()
        {
            // Arrange
            var context = CreateHttpContext();
            var wasCalled = false;

            var middleware = new SmartPlanner.API.Middleware.GlobalExceptionHandlingMiddleware(
                next: (innerContext) =>
                {
                    wasCalled = true;
                    innerContext.Response.StatusCode = StatusCodes.Status200OK;
                    return Task.CompletedTask;
                },
                logger: _mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(wasCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }
}
