using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Auth.Commands
{
    public class RefreshTokenCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ILogger<RefreshTokenCommandHandler>> _mockLogger;
        private readonly RefreshTokenCommandHandler _handler;

        public RefreshTokenCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = new Mock<ILogger<RefreshTokenCommandHandler>>();

            _handler = new RefreshTokenCommandHandler(
                _mockContext.Object,
                _mockTokenService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_OldSessionNotFound_ThrowsUnauthorizedException()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                AccessToken = "expired-token",
                RefreshToken = "invalid-refresh-token"
            };

            _mockTokenService.Setup(x => x.GetSessionByRefreshTokenAsync(
                command.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSession)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_InvalidPrincipal_ThrowsUnauthorizedException()
        {
            // Arrange
            var command = new RefreshTokenCommand
            {
                AccessToken = "expired-token",
                RefreshToken = "refresh-token"
            };

            var oldSession = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DeviceInfo = "Device",
                IpAddress = "127.0.0.1"
            };

            _mockTokenService.Setup(x => x.GetSessionByRefreshTokenAsync(
                command.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(oldSession);

            _mockTokenService.Setup(x => x.GetPrincipalFromExpiredToken(command.AccessToken))
                .Returns((ClaimsPrincipal)null);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ValidRefresh_ReturnsNewTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new RefreshTokenCommand
            {
                AccessToken = "expired-token",
                RefreshToken = "old-refresh-token"
            };

            var oldSession = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceInfo = "Test Device",
                IpAddress = "127.0.0.1"
            };

            var claims = new List<Claim>
            {
                new Claim("userId", userId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = "User",
                RolePermissions = new List<RolePermission>()
            };

            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                Username = "testuser",
                FirstName = "John",
                LastName = "Doe",
                IsActive = true,
                IsDeleted = false,
                UserRoles = new List<UserRole>
                {
                    new UserRole { Role = role }
                }
            };

            _mockTokenService.Setup(x => x.GetSessionByRefreshTokenAsync(
                command.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(oldSession);

            _mockTokenService.Setup(x => x.GetPrincipalFromExpiredToken(command.AccessToken))
                .Returns(principal);

            _mockTokenService.Setup(x => x.ValidateRefreshTokenAsync(
                command.RefreshToken, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockTokenService.Setup(x => x.RevokeSessionAsync(
                command.RefreshToken, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(
                user, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new-access-token");

            _mockTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(("new-refresh-token", "new-refresh-hash"));

            _mockTokenService.Setup(x => x.CreateUserSessionAsync(
                    userId,
                    "new-refresh-hash",
                    It.IsAny<DateTime>(),
                    oldSession.DeviceInfo,
                    oldSession.IpAddress,
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            var mockUsersDbSet = new Mock<DbSet<User>>();
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Provider)
                .Returns(new List<User> { user }.AsQueryable().Provider);
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Expression)
                .Returns(new List<User> { user }.AsQueryable().Expression);
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.ElementType)
                .Returns(new List<User> { user }.AsQueryable().ElementType);
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.GetEnumerator())
                .Returns(new List<User> { user }.GetEnumerator());

            mockUsersDbSet.Setup(x => x.Include(It.IsAny<string>()))
                .Returns(mockUsersDbSet.Object);

            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("new-refresh-token");
            result.User.Id.Should().Be(userId);

            _mockTokenService.Verify(x => x.RevokeSessionAsync(
                command.RefreshToken, It.IsAny<CancellationToken>()), Times.Once);

            _mockTokenService.Verify(x => x.CreateUserSessionAsync(
                userId,
                "new-refresh-hash",
                It.IsAny<DateTime>(),
                oldSession.DeviceInfo,
                oldSession.IpAddress,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
