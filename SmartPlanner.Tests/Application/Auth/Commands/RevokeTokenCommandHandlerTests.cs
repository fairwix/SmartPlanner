// Tests/Application/Auth/Commands/RevokeTokenCommandHandlerTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Auth.Commands;

public class RevokeTokenCommandHandlerTests : AuthCommandTestBase
{
    private readonly RevokeTokenCommandHandler _handler;
    private readonly Mock<ILogger<RevokeTokenCommandHandler>> _mockLogger;

    public RevokeTokenCommandHandlerTests()
    {
        _mockLogger = new Mock<ILogger<RevokeTokenCommandHandler>>();

        _handler = new RevokeTokenCommandHandler(
            MockTokenService.Object,
            MockContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFalse()
    {
        // Arrange
        var users = new List<User>();
        var mockSet = CreateMockDbSet(users);
        MockContext.Setup(c => c.Users).Returns(mockSet.Object);

        var command = new RevokeTokenCommand
        {
            UserId = Guid.NewGuid(),
            RefreshToken = "refresh_token"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFalse()
    {
        // Arrange
        var user = CreateTestUser();
        var users = new List<User> { user };
        var mockSet = CreateMockDbSet(users);

        MockContext.Setup(c => c.Users).Returns(mockSet.Object);
        MockTokenService.Setup(t => t.GetSessionByRefreshTokenAsync(
            "refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession)null);

        var command = new RevokeTokenCommand
        {
            UserId = user.Id,
            RefreshToken = "refresh_token"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TokenBelongsToDifferentUser_ReturnsFalse()
    {
        // Arrange
        var user = CreateTestUser();
        var otherUserId = Guid.NewGuid();
        var session = new UserSession { UserId = otherUserId };

        var users = new List<User> { user };
        var mockSet = CreateMockDbSet(users);

        MockContext.Setup(c => c.Users).Returns(mockSet.Object);
        MockTokenService.Setup(t => t.GetSessionByRefreshTokenAsync(
            "refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new RevokeTokenCommand
        {
            UserId = user.Id,
            RefreshToken = "refresh_token"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidRevoke_ReturnsTrue()
    {
        // Arrange
        var user = CreateTestUser();
        var session = new UserSession { UserId = user.Id };

        var users = new List<User> { user };
        var mockSet = CreateMockDbSet(users);

        MockContext.Setup(c => c.Users).Returns(mockSet.Object);
        MockTokenService.Setup(t => t.GetSessionByRefreshTokenAsync(
            "refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        MockTokenService.Setup(t => t.RevokeSessionAsync(
            "refresh_token", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new RevokeTokenCommand
        {
            UserId = user.Id,
            RefreshToken = "refresh_token"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        MockTokenService.Verify(t => t.RevokeSessionAsync(
            "refresh_token", It.IsAny<CancellationToken>()), Times.Once);
    }
}
