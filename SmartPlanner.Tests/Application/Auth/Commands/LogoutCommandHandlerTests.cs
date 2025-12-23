using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Auth.Commands
{
    public class LogoutCommandHandlerTests
    {
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ILogger<LogoutCommandHandler>> _mockLogger;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly LogoutCommandHandler _handler;

        public LogoutCommandHandlerTests()
        {
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = new Mock<ILogger<LogoutCommandHandler>>();
            _mockAuditService = new Mock<IAuditService>();

            _handler = new LogoutCommandHandler(
                _mockTokenService.Object,
                _mockLogger.Object,
                _mockAuditService.Object
            );
        }

        [Fact]
        public async Task Handle_ValidLogout_ReturnsUnitValue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new LogoutCommand(userId);

            _mockTokenService.Setup(x => x.RevokeUserSessionsAsync(
                userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Исходя из кода LogoutCommandHandler:
            // await _auditService.LogSecurityEventAsync(
            //     SecurityEventType.Logout,
            //     request.UserId,
            //     ipAddress: null, // ← ЧЕТВЕРТЫЙ параметр
            //     success: true,   // ← ПЯТЫЙ параметр
            //     cancellationToken: cancellationToken);

            // Это означает, что сигнатура метода в IAuditService:
            // Task LogSecurityEventAsync(
            //     SecurityEventType eventType,
            //     Guid userId,
            //     string? email = null,        // ← ТРЕТИЙ параметр (но в вызове он пропущен)
            //     string? ipAddress = null,    // ← ЧЕТВЕРТЫЙ параметр
            //     string? userAgent = null,    // ← ПЯТЫЙ параметр (но в вызове он пропущен)
            //     bool success = true,         // ← ШЕСТОЙ параметр
            //     object? details = null,      // ← СЕДЬМОЙ параметр (но в вызове он пропущен)
            //     CancellationToken cancellationToken = default);

            // В вызове пропущены email, userAgent, details, поэтому передаем null/default

            _mockAuditService.Setup(x => x.LogSecurityEventAsync(
                SecurityEventType.Logout,        // 1. eventType
                userId,                          // 2. userId
                null,                            // 3. email (пропущен в вызове → null)
                null,                            // 4. ipAddress (в вызове: ipAddress: null)
                null,                            // 5. userAgent (пропущен в вызове → null)
                true,                            // 6. success (в вызове: success: true)
                null,                            // 7. details (пропущен в вызове → null)
                It.IsAny<CancellationToken>()))  // 8. cancellationToken
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(Unit.Value);
            _mockTokenService.Verify(x => x.RevokeUserSessionsAsync(
                userId, It.IsAny<CancellationToken>()), Times.Once);

            // Для Verify используем ту же сигнатуру
            _mockAuditService.Verify(x => x.LogSecurityEventAsync(
                SecurityEventType.Logout,
                userId,
                null,    // email
                null,    // ipAddress
                null,    // userAgent
                true,    // success
                null,    // details
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_LogoutWithItIsAny_ReturnsUnitValue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new LogoutCommand(userId);

            _mockTokenService.Setup(x => x.RevokeUserSessionsAsync(
                userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Альтернативный способ - использовать It.IsAny<> для всех параметров
            // Это избежит проблем с порядком и типами параметров
            _mockAuditService.Setup(x => x.LogSecurityEventAsync(
                It.IsAny<SecurityEventType>(),   // eventType
                It.IsAny<Guid>(),                // userId
                It.IsAny<string>(),              // email (string?)
                It.IsAny<string>(),              // ipAddress (string?)
                It.IsAny<string>(),              // userAgent (string?)
                It.IsAny<bool>(),                // success
                It.IsAny<object>(),              // details
                It.IsAny<CancellationToken>()))  // cancellationToken
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(Unit.Value);

            _mockAuditService.Verify(x => x.LogSecurityEventAsync(
                It.IsAny<SecurityEventType>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),    // email
                It.IsAny<string>(),    // ipAddress
                It.IsAny<string>(),    // userAgent
                It.IsAny<bool>(),      // success
                It.IsAny<object>(),    // details
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TokenServiceThrows_StillLogsAudit()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new LogoutCommand(userId);

            // TokenService выбрасывает исключение
            _mockTokenService.Setup(x => x.RevokeUserSessionsAsync(
                userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            _mockAuditService.Setup(x => x.LogSecurityEventAsync(
                It.IsAny<SecurityEventType>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),    // email
                It.IsAny<string>(),    // ipAddress
                It.IsAny<string>(),    // userAgent
                It.IsAny<bool>(),      // success
                It.IsAny<object>(),    // details
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert - должно бросить исключение
            await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));

            // Но audit все равно должен быть записан
            _mockAuditService.Verify(x => x.LogSecurityEventAsync(
                It.IsAny<SecurityEventType>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NullParameters_HandlesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new LogoutCommand(userId);

            _mockTokenService.Setup(x => x.RevokeUserSessionsAsync(
                userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Проверяем обработку null параметров
            _mockAuditService.Setup(x => x.LogSecurityEventAsync(
                SecurityEventType.Logout,
                userId,
                It.Is<string>(e => e == null),    // email должен быть null
                It.Is<string>(ip => ip == null),  // ipAddress должен быть null
                It.Is<string>(ua => ua == null),  // userAgent должен быть null
                true,                             // success = true
                It.Is<object>(d => d == null),    // details должен быть null
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(Unit.Value);
        }
    }
}
