// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Moq;
// using SmartPlanner.Application.Auth.Commands;
// using SmartPlanner.Application.Auth.Dtos;
// using SmartPlanner.Application.Auth.Interfaces;
// using SmartPlanner.Application.Auth.Services;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Application.Common.Models;
// using SmartPlanner.Application.Security.Services;
// using SmartPlanner.Domain.Entities;
// using Xunit;
//
// namespace SmartPlanner.Application.Tests.Auth.Commands
// {
//     public class ConfirmEmailCommandHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _mockContext;
//         private readonly Mock<IConfirmationTokenService> _mockTokenService;
//         private readonly Mock<ILogger<ConfirmEmailCommandHandler>> _mockLogger;
//         private readonly Mock<IEmailService> _mockEmailService;
//         private readonly Mock<IAuditService> _mockAuditService;
//         private readonly Mock<IOptions<AppSettings>> _mockAppSettings;
//         private readonly ConfirmEmailCommandHandler _handler;
//
//         public ConfirmEmailCommandHandlerTests()
//         {
//             _mockContext = new Mock<IApplicationDbContext>();
//             _mockTokenService = new Mock<IConfirmationTokenService>();
//             _mockLogger = new Mock<ILogger<ConfirmEmailCommandHandler>>();
//             _mockEmailService = new Mock<IEmailService>();
//             _mockAuditService = new Mock<IAuditService>();
//             _mockAppSettings = new Mock<IOptions<AppSettings>>();
//
//             var appSettings = new AppSettings
//             {
//                 FrontendUrls = new FrontendUrls
//                 {
//                     LoginUrl = "/login",
//                     DashboardUrl = "/dashboard"
//                 }
//             };
//
//             _mockAppSettings.Setup(x => x.Value).Returns(appSettings);
//
//             _handler = new ConfirmEmailCommandHandler(
//                 _mockContext.Object,
//                 _mockTokenService.Object,
//                 _mockLogger.Object,
//                 _mockEmailService.Object,
//                 _mockAuditService.Object,
//                 _mockAppSettings.Object
//             );
//         }
//
//         [Fact]
//         public async Task Handle_UserNotFound_ReturnsFailure()
//         {
//             // Arrange
//             var command = new ConfirmEmailCommand
//             {
//                 UserId = Guid.NewGuid(),
//                 Token = "confirmation-token"
//             };
//
//             var mockUsersDbSet = new Mock<DbSet<User>>();
//             _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
//
//             _mockAuditService.Setup(x => x.LogSecurityEventAsync(
//                 It.IsAny<SecurityEventType>(),
//                 It.IsAny<Guid>(),
//                 It.IsAny<bool>(),
//                 It.IsAny<object>(),
//                 It.IsAny<CancellationToken>()))
//                 .Returns(Task.CompletedTask);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             result.Success.Should().BeFalse();
//             result.Message.Should().Contain("User not found");
//         }
//
//         [Fact]
//         public async Task Handle_EmailAlreadyConfirmed_ReturnsSuccess()
//         {
//             // Arrange
//             var command = new ConfirmEmailCommand
//             {
//                 UserId = Guid.NewGuid(),
//                 Token = "confirmation-token"
//             };
//
//             var user = new User
//             {
//                 Id = command.UserId,
//                 Email = "user@example.com",
//                 IsActive = true,
//                 IsDeleted = false,
//                 IsEmailConfirmed = true,
//                 EmailConfirmedAt = DateTime.UtcNow.AddDays(-1)
//             };
//
//             var mockUsersDbSet = new Mock<DbSet<User>>();
//             mockUsersDbSet.As<IQueryable<User>>()
//                 .Setup(x => x.Provider)
//                 .Returns(new List<User> { user }.AsQueryable().Provider);
//
//             _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             result.Success.Should().BeTrue();
//             result.Message.Should().Contain("already confirmed");
//             result.RedirectUrl.Should().Be("/login");
//         }
//
//         [Fact]
//         public async Task Handle_InvalidToken_ReturnsFailure()
//         {
//             // Arrange
//             var command = new ConfirmEmailCommand
//             {
//                 UserId = Guid.NewGuid(),
//                 Token = "invalid-token"
//             };
//
//             var user = new User
//             {
//                 Id = command.UserId,
//                 Email = "user@example.com",
//                 IsActive = true,
//                 IsDeleted = false,
//                 IsEmailConfirmed = false
//             };
//
//             var mockUsersDbSet = new Mock<DbSet<User>>();
//             var mockEmailTokensDbSet = new Mock<DbSet<EmailConfirmationToken>>();
//
//             mockUsersDbSet.As<IQueryable<User>>()
//                 .Setup(x => x.Provider)
//                 .Returns(new List<User> { user }.AsQueryable().Provider);
//
//             _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
//             _mockContext.Setup(x => x.EmailConfirmationTokens).Returns(mockEmailTokensDbSet.Object);
//             _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .Returns(Task.FromResult(1));
//
//             _mockTokenService.Setup(x => x.ValidateEmailConfirmationTokenAsync(
//                 command.Token, user.Id, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false);
//
//             _mockAuditService.Setup(x => x.LogSecurityEventAsync(
//                 It.IsAny<SecurityEventType>(),
//                 It.IsAny<Guid>(),
//                 It.IsAny<string>(),
//                 It.IsAny<bool>(),
//                 It.IsAny<object>(),
//                 It.IsAny<CancellationToken>()))
//                 .Returns(Task.CompletedTask);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             result.Success.Should().BeFalse();
//             result.Message.Should().Contain("Invalid or expired");
//         }
//
//         [Fact]
//         public async Task Handle_ValidConfirmation_ReturnsSuccess()
//         {
//             // Arrange
//             var command = new ConfirmEmailCommand
//             {
//                 UserId = Guid.NewGuid(),
//                 Token = "valid-token"
//             };
//
//             var user = new User
//             {
//                 Id = command.UserId,
//                 Email = "user@example.com",
//                 Username = "testuser",
//                 IsActive = true,
//                 IsDeleted = false,
//                 IsEmailConfirmed = false,
//                 EmailConfirmedAt = null
//             };
//
//             var mockUsersDbSet = new Mock<DbSet<User>>();
//             var mockEmailTokensDbSet = new Mock<DbSet<EmailConfirmationToken>>();
//
//             mockUsersDbSet.As<IQueryable<User>>()
//                 .Setup(x => x.Provider)
//                 .Returns(new List<User> { user }.AsQueryable().Provider);
//
//             _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
//             _mockContext.Setup(x => x.EmailConfirmationTokens).Returns(mockEmailTokensDbSet.Object);
//             _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .Returns(Task.FromResult(1));
//
//             _mockTokenService.Setup(x => x.ValidateEmailConfirmationTokenAsync(
//                 command.Token, user.Id, It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(true);
//
//             _mockAuditService.Setup(x => x.LogSecurityEventAsync(
//                 SecurityEventType.EmailConfirmed,
//                 user.Id,
//                 user.Email,
//                 true,
//                 It.IsAny<CancellationToken>()))
//                 .Returns(Task.CompletedTask);
//
//             _mockEmailService.Setup(x => x.SendWelcomeEmailAsync(
//                 user.Email, user.Username))
//                 .Returns(Task.CompletedTask);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             result.Success.Should().BeTrue();
//             result.Message.Should().Contain("successfully confirmed");
//             result.RedirectUrl.Should().Be("/dashboard");
//             _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//             _mockAuditService.Verify(x => x.LogSecurityEventAsync(
//                 SecurityEventType.EmailConfirmed,
//                 user.Id,
//                 user.Email,
//                 true,
//                 It.IsAny<CancellationToken>()), Times.Once);
//         }
//     }
// }
