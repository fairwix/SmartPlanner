// SmartPlanner.Tests/API/Controllers/AuthControllerTests.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SmartPlanner.API.Controllers;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Application.Auth.Dtos;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using SmartPlanner.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Services;

namespace SmartPlanner.Tests.API.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly AuthController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AuthControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockAuditService = new Mock<IAuditService>();

            _controller = new AuthController(
                _mockMediator.Object,
                _mockLogger.Object
            );

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var registerDto = new RegisterDto(
                "test@example.com",
                "testuser",
                "Password123!",
                "Password123!",
                "Test",
                "User",
                null,
                null
            );

            var authResponse = new AuthResponseDto(
                "access_token",
                "refresh_token",
                DateTime.UtcNow.AddMinutes(15),
                DateTime.UtcNow.AddDays(7),
                new UserProfileDto(
                    Guid.NewGuid(),
                    "test@example.com",
                    "testuser",
                    "Test",
                    "User",
                    null,
                    null,
                    DateTime.UtcNow,
                    null,
                    new List<string> { "User" },
                    new List<string>()));

            _mockMediator.Setup(m => m.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Register(registerDto, CancellationToken.None);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<AuthResponseDto>(createdAtResult.Value);
            Assert.Equal("access_token", response.AccessToken);
            _mockMediator.Verify(m => m.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var loginDto = new LoginDto("testuser", "Password123!");

            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            _httpContext.Request.Headers["User-Agent"] = "TestAgent";

            var authResponse = new AuthResponseDto(
                "access_token",
                "refresh_token",
                DateTime.UtcNow.AddMinutes(15),
                DateTime.UtcNow.AddDays(7),
                new UserProfileDto(
                    Guid.NewGuid(),
                    "test@example.com",
                    "testuser",
                    "Test",
                    "User",
                    null,
                    null,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    new List<string> { "User" },
                    new List<string>()));

            _mockMediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Login(loginDto, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthResponseDto>(okResult.Value);
            Assert.Equal("access_token", response.AccessToken);
            _mockMediator.Verify(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto("testuser", "wrongpassword");

            _mockMediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.Login(loginDto, CancellationToken.None));
        }

        [Fact]
        public async Task ChangePassword_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto(
                "CurrentPassword123!",
                "NewPassword123!",
                "NewPassword123!"
            );

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim("userId", Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            _mockMediator.Setup(m => m.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(changePasswordDto, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Dictionary<string, string>>(okResult.Value);
            Assert.Contains("message", response);
            Assert.Equal("Password changed successfully.", response["message"]);
        }

        [Fact]
        public async Task ForgotPassword_ValidEmail_ReturnsOkResult()
        {
            // Arrange
            var forgotPasswordDto = new ForgotPasswordDto("test@example.com");

            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            _httpContext.Request.Headers["User-Agent"] = "TestAgent";

            _mockMediator.Setup(m => m.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
                .Returns((Task<bool>)Task.CompletedTask);

            // Act
            var result = await _controller.ForgotPassword(forgotPasswordDto, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Dictionary<string, string>>(okResult.Value);
            Assert.Contains("message", response);
            Assert.Equal("If your email exists in our system, you will receive a password reset link.", response["message"]);
        }
    }
}
