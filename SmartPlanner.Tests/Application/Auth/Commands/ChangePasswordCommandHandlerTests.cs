using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Auth.Commands
{
    public class ChangePasswordCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ILogger<ChangePasswordCommandHandler>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly ChangePasswordCommandHandler _handler;

        public ChangePasswordCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = new Mock<ILogger<ChangePasswordCommandHandler>>();
            _mockEmailService = new Mock<IEmailService>();

            _handler = new ChangePasswordCommandHandler(
                _mockContext.Object,
                _mockPasswordHasher.Object,
                _mockTokenService.Object,
                _mockLogger.Object,
                _mockEmailService.Object
            );
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = Guid.NewGuid(),
                CurrentPassword = "CurrentPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };

            var mockUsersDbSet = new Mock<DbSet<User>>();
            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_IncorrectCurrentPassword_ReturnsFalse()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = Guid.NewGuid(),
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };

            var user = new User
            {
                Id = command.UserId,
                Email = "user@example.com",
                IsActive = true,
                IsDeleted = false,
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            var mockUsersDbSet = new Mock<DbSet<User>>();
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Provider)
                .Returns(new List<User> { user }.AsQueryable().Provider);

            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                .Returns(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NewPasswordSameAsCurrent_ReturnsFalse()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = Guid.NewGuid(),
                CurrentPassword = "Password123!",
                NewPassword = "Password123!",
                ConfirmNewPassword = "Password123!"
            };

            var user = new User
            {
                Id = command.UserId,
                Email = "user@example.com",
                IsActive = true,
                IsDeleted = false,
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            var mockUsersDbSet = new Mock<DbSet<User>>();
            mockUsersDbSet.As<IQueryable<User>>()
                .Setup(x => x.Provider)
                .Returns(new List<User> { user }.AsQueryable().Provider);

            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                .Returns(true);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.NewPassword, user.PasswordHash, user.PasswordSalt))
                .Returns(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        // [Fact]
        // public async Task Handle_ValidChange_ReturnsTrue()
        // {
        //     // Arrange
        //     var command = new ChangePasswordCommand
        //     {
        //         UserId = Guid.NewGuid(),
        //         CurrentPassword = "CurrentPassword123!",
        //         NewPassword = "NewPassword123!",
        //         ConfirmNewPassword = "NewPassword123!"
        //     };
        //
        //     var user = new User
        //     {
        //         Id = command.UserId,
        //         Email = "user@example.com",
        //         IsActive = true,
        //         IsDeleted = false,
        //         PasswordHash = "old-hash",
        //         PasswordSalt = "old-salt"
        //     };
        //
        //     var mockUsersDbSet = new Mock<DbSet<User>>();
        //     mockUsersDbSet.As<IQueryable<User>>()
        //         .Setup(x => x.Provider)
        //         .Returns(new List<User> { user }.AsQueryable().Provider);
        //
        //     _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
        //     _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        //         .Returns(Task.FromResult(1));
        //
        //     _mockPasswordHasher.SetupSequence(x => x.VerifyPassword(
        //         It.IsAny<string>(), user.PasswordHash, user.PasswordSalt))
        //         .Returns(true) // For current password
        //         .Returns(false); // For new password (different)
        //
        //     _mockPasswordHasher.Setup(x => x.HashPassword(command.NewPassword))
        //         .Returns(("new-hash", "new-salt"));
        //
        //     _mockTokenService.Setup(x => x.RevokeUserSessionsAsync(
        //         user.Id, It.IsAny<CancellationToken>()))
        //         .Returns(Task.CompletedTask);
        //
        //     _mockEmailService.Setup(x => x.SendEmailAsync(
        //         user.Email,
        //         It.IsAny<string>(),
        //         It.IsAny<string>()))
        //         .Returns(Task.CompletedTask);
        //
        //     // Act
        //     var result = await _handler.Handle(command, CancellationToken.None);
        //
        //     // Assert
        //     result.Should().BeTrue();
        //     _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        //     _mockTokenService.Verify(x => x.RevokeUserSessionsAsync(
        //         user.Id, It.IsAny<CancellationToken>()), Times.Once);
        //     _mockEmailService.Verify(x => x.SendEmailAsync(
        //         user.Email,
        //         It.IsAny<string>(),
        //         It.IsAny<string>()), Times.Once);
        // }
    }
}
