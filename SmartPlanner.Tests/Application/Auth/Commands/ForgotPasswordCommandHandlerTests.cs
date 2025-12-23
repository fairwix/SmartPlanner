using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Auth.Commands
{
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfirmationTokenService> _mockTokenService;
        private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _mockLogger;
        private readonly ForgotPasswordCommandHandler _handler;

        public ForgotPasswordCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockEmailService = new Mock<IEmailService>();
            _mockTokenService = new Mock<IConfirmationTokenService>();
            _mockLogger = new Mock<ILogger<ForgotPasswordCommandHandler>>();

            _handler = new ForgotPasswordCommandHandler(
                _mockContext.Object,
                _mockEmailService.Object,
                _mockTokenService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_NonExistentEmail_ReturnsTrueForSecurity()
        {
            // Arrange
            var command = new ForgotPasswordCommand
            {
                Email = "nonexistent@example.com",
                IpAddress = "127.0.0.1",
                UserAgent = "Test Browser"
            };

            var mockUsersDbSet = new Mock<DbSet<User>>();
            mockUsersDbSet.Setup(x => x.AsNoTracking()).Returns(mockUsersDbSet.Object);

            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_TooManyRequests_ReturnsTrue()
        {
            // Arrange
            var command = new ForgotPasswordCommand
            {
                Email = "user@example.com",
                IpAddress = "127.0.0.1",
                UserAgent = "Test Browser"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                IsActive = true,
                IsDeleted = false
            };

            var mockUsersDbSet = new Mock<DbSet<User>>();
            var mockPasswordResetTokensDbSet = new Mock<DbSet<PasswordResetToken>>();

            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Provider)
                .Returns(new List<User> { user }.AsQueryable().Provider);
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Expression)
                .Returns(new List<User> { user }.AsQueryable().Expression);
            mockUsersDbSet.Setup(x => x.AsNoTracking()).Returns(mockUsersDbSet.Object);

            mockPasswordResetTokensDbSet.Setup(x => x.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PasswordResetToken, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(5); // More than 3 requests

            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
            _mockContext.Setup(x => x.PasswordResetTokens).Returns(mockPasswordResetTokensDbSet.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _mockTokenService.Verify(x => x.GeneratePasswordResetTokenAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRequest_SendsResetEmail()
        {
            // Arrange
            var command = new ForgotPasswordCommand
            {
                Email = "user@example.com",
                IpAddress = "127.0.0.1",
                UserAgent = "Test Browser"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                Username = "testuser",
                IsActive = true,
                IsDeleted = false
            };

            var mockUsersDbSet = new Mock<DbSet<User>>();
            var mockPasswordResetTokensDbSet = new Mock<DbSet<PasswordResetToken>>();

            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Provider)
                .Returns(new List<User> { user }.AsQueryable().Provider);
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Expression)
                .Returns(new List<User> { user }.AsQueryable().Expression);
            mockUsersDbSet.Setup(x => x.AsNoTracking()).Returns(mockUsersDbSet.Object);

            mockPasswordResetTokensDbSet.Setup(x => x.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PasswordResetToken, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
            _mockContext.Setup(x => x.PasswordResetTokens).Returns(mockPasswordResetTokensDbSet.Object);

            _mockTokenService.Setup(x => x.GeneratePasswordResetTokenAsync(
                user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync("reset-token");

            _mockEmailService.Setup(x => x.SendPasswordResetAsync(
                user.Email, user.Username, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _mockTokenService.Verify(x => x.GeneratePasswordResetTokenAsync(
                user.Id, It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(x => x.SendPasswordResetAsync(
                user.Email, user.Username, It.IsAny<string>()), Times.Once);
        }
    }
}
