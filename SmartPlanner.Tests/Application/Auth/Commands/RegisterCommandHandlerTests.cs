// using MediatR;
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
// namespace SmartPlanner.Tests.Application.Auth.Commands
// {
//     public class RegisterCommandHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _mockContext;
//         private readonly Mock<ITokenService> _mockTokenService;
//         private readonly Mock<IPasswordHasher> _mockPasswordHasher;
//         private readonly Mock<ILogger<RegisterCommandHandler>> _mockLogger;
//         private readonly Mock<IConfirmationTokenService> _mockConfirmationTokenService;
//         private readonly Mock<IEmailService> _mockEmailService;
//         private readonly Mock<IAuditService> _mockAuditService;
//         private readonly AppSettings _appSettings;
//         private readonly RegisterCommandHandler _handler;
//
//         public RegisterCommandHandlerTests()
//         {
//             _mockContext = new Mock<IApplicationDbContext>();
//             _mockTokenService = new Mock<ITokenService>();
//             _mockPasswordHasher = new Mock<IPasswordHasher>();
//             _mockLogger = new Mock<ILogger<RegisterCommandHandler>>();
//             _mockConfirmationTokenService = new Mock<IConfirmationTokenService>();
//             _mockEmailService = new Mock<IEmailService>();
//             _mockAuditService = new Mock<IAuditService>();
//
//             _appSettings = new AppSettings
//             {
//                 BaseUrl = "https://example.com",
//                 Jwt = new JwtSettings
//                 {
//                     AccessTokenExpirationMinutes = 15,
//                     RefreshTokenExpirationDays = 7
//                 }
//             };
//
//             var mockAppSettings = new Mock<IOptions<AppSettings>>();
//             mockAppSettings.Setup(x => x.Value).Returns(_appSettings);
//
//             _handler = new RegisterCommandHandler(
//                 _mockContext.Object,
//                 _mockTokenService.Object,
//                 _mockPasswordHasher.Object,
//                 _mockLogger.Object,
//                 _mockConfirmationTokenService.Object,
//                 _mockEmailService.Object,
//                 _mockAuditService.Object,
//                 mockAppSettings.Object);
//         }
//
//         private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
//         {
//             var mockSet = new Mock<DbSet<T>>();
//             var queryable = data.AsQueryable();
//
//             mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
//             mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
//             mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
//             mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
//
//             return mockSet;
//         }
//
//         [Fact]
//         public async Task Handle_ValidRegistration_ReturnsAuthResponse()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "test@example.com",
//                 username: "testuser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!",
//                 firstName: "John",
//                 lastName: "Doe",
//                 dateOfBirth: new DateTime(1990, 1, 1),
//                 phoneNumber: "+1234567890");
//
//             var users = new List<User>();
//             var userRoles = new List<UserRole>();
//             var roles = new List<Role>
//             {
//                 new Role { Id = Guid.NewGuid(), Name = "User" }
//             };
//
//             var mockUsersSet = CreateMockDbSet(users);
//             var mockRolesSet = CreateMockDbSet(roles);
//             var mockUserRolesSet = CreateMockDbSet(userRoles);
//
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//             _mockContext.Setup(c => c.Roles).Returns(mockRolesSet.Object);
//             _mockContext.Setup(c => c.UserRoles).Returns(mockUserRolesSet.Object);
//
//             _mockContext.Setup(c => c.Users.AnyAsync(
//                 It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(e =>
//                     e.Compile()(new User { Email = "test@example.com" })),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false);
//
//             _mockContext.Setup(c => c.Users.AnyAsync(
//                 It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(e =>
//                     e.Compile()(new User { Username = "testuser" })),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false);
//
//             _mockPasswordHasher.Setup(p => p.HashPassword("Password123!"))
//                 .Returns(("hashed_password", "salt"));
//
//             _mockTokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("access_token");
//
//             _mockTokenService.Setup(t => t.GenerateRefreshToken())
//                 .Returns(("refresh_token", "refresh_token_hash"));
//
//             _mockTokenService.Setup(t => t.CreateUserSessionAsync(
//                 It.IsAny<Guid>(),
//                 It.IsAny<string>(),
//                 It.IsAny<DateTime>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new UserSession());
//
//             _mockConfirmationTokenService.Setup(c => c.GenerateEmailConfirmationTokenAsync(
//                 It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("confirmation_token");
//
//             _mockEmailService.Setup(e => e.SendEmailConfirmationAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>()))
//                 .Returns(Task.CompletedTask);
//
//             _mockAuditService.Setup(a => a.LogSecurityEventAsync(
//                 It.IsAny<SecurityEventType>(),
//                 It.IsAny<Guid?>(),
//                 It.IsAny<string?>(),
//                 It.IsAny<bool>(),
//                 It.IsAny<object?>(),
//                 It.IsAny<CancellationToken>()))
//                 .Returns(Task.CompletedTask);
//
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal("access_token", result.AccessToken);
//             Assert.Equal("refresh_token", result.RefreshToken);
//             Assert.Equal("test@example.com", result.User.Email);
//             Assert.Equal("testuser", result.User.Username);
//             Assert.Equal("John", result.User.FirstName);
//             Assert.Equal("Doe", result.User.LastName);
//             Assert.Contains("User", result.User.Roles);
//
//             _mockContext.Verify(c => c.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
//             _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
//             _mockEmailService.Verify(e => e.SendEmailConfirmationAsync(
//                 "test@example.com", "testuser", It.IsAny<string>()), Times.Once);
//             _mockAuditService.Verify(a => a.LogSecurityEventAsync(
//                 SecurityEventType.Register,
//                 It.IsAny<Guid?>(),
//                 "test@example.com",
//                 true,
//                 It.IsAny<object?>(),
//                 It.IsAny<CancellationToken>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task Handle_EmailAlreadyExists_ThrowsArgumentException()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "existing@example.com",
//                 username: "newuser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!");
//
//             var users = new List<User>
//             {
//                 new User { Email = "existing@example.com" }
//             };
//
//             var mockUsersSet = CreateMockDbSet(users);
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//
//             _mockContext.Setup(c => c.Users.AnyAsync(
//                 It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(e =>
//                     e.Compile()(new User { Email = "existing@example.com" })),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(true);
//
//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<ArgumentException>(
//                 () => _handler.Handle(command, CancellationToken.None));
//
//             Assert.Contains("already registered", exception.Message);
//         }
//
//         [Fact]
//         public async Task Handle_UsernameAlreadyExists_ThrowsArgumentException()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "new@example.com",
//                 username: "existinguser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!");
//
//             var users = new List<User>
//             {
//                 new User { Username = "existinguser" }
//             };
//
//             var mockUsersSet = CreateMockDbSet(users);
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//
//             _mockContext.SetupSequence(c => c.Users.AnyAsync(
//                 It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false) // Email не существует
//                 .ReturnsAsync(true); // Username существует
//
//             // Act & Assert
//             var exception = await Assert.ThrowsAsync<ArgumentException>(
//                 () => _handler.Handle(command, CancellationToken.None));
//
//             Assert.Contains("already taken", exception.Message);
//         }
//
//         [Fact]
//         public async Task Handle_UserRoleNotFound_StillCreatesUser()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "test@example.com",
//                 username: "testuser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!");
//
//             var users = new List<User>();
//             var roles = new List<Role>(); // Пустой список - роль "User" не найдена
//
//             var mockUsersSet = CreateMockDbSet(users);
//             var mockRolesSet = CreateMockDbSet(roles);
//
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//             _mockContext.Setup(c => c.Roles).Returns(mockRolesSet.Object);
//
//             _mockContext.SetupSequence(c => c.Users.AnyAsync(
//                 It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false)
//                 .ReturnsAsync(false);
//
//             _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<string>()))
//                 .Returns(("hash", "salt"));
//
//             _mockTokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("access_token");
//
//             _mockTokenService.Setup(t => t.GenerateRefreshToken())
//                 .Returns(("refresh_token", "refresh_token_hash"));
//
//             _mockTokenService.Setup(t => t.CreateUserSessionAsync(
//                 It.IsAny<Guid>(),
//                 It.IsAny<string>(),
//                 It.IsAny<DateTime>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new UserSession());
//
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Empty(result.User.Roles); // Роли не назначены
//             _mockContext.Verify(c => c.UserRoles.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task Handle_EmailConfirmationTokenGenerated()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "test@example.com",
//                 username: "testuser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!");
//
//             var users = new List<User>();
//             var roles = new List<Role>
//             {
//                 new Role { Id = Guid.NewGuid(), Name = "User" }
//             };
//
//             var mockUsersSet = CreateMockDbSet(users);
//             var mockRolesSet = CreateMockDbSet(roles);
//
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//             _mockContext.Setup(c => c.Roles).Returns(mockRolesSet.Object);
//
//             _mockContext.SetupSequence(c => c.Users.AnyAsync(
//                 It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false)
//                 .ReturnsAsync(false);
//
//             _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<string>()))
//                 .Returns(("hash", "salt"));
//
//             _mockTokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("access_token");
//
//             _mockTokenService.Setup(t => t.GenerateRefreshToken())
//                 .Returns(("refresh_token", "refresh_token_hash"));
//
//             _mockTokenService.Setup(t => t.CreateUserSessionAsync(
//                 It.IsAny<Guid>(),
//                 It.IsAny<string>(),
//                 It.IsAny<DateTime>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new UserSession());
//
//             _mockConfirmationTokenService.Setup(c => c.GenerateEmailConfirmationTokenAsync(
//                 It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("confirmation_token");
//
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             // Act
//             await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             _mockConfirmationTokenService.Verify(
//                 c => c.GenerateEmailConfirmationTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
//                 Times.Once);
//
//             _mockEmailService.Verify(
//                 e => e.SendEmailConfirmationAsync(
//                     "test@example.com",
//                     "testuser",
//                     It.Is<string>(s => s.Contains("confirmation_token"))),
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task Handle_EmailSendingFailure_StillReturnsResponse()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "test@example.com",
//                 username: "testuser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!");
//
//             var users = new List<User>();
//             var roles = new List<Role>
//             {
//                 new Role { Id = Guid.NewGuid(), Name = "User" }
//             };
//
//             var mockUsersSet = CreateMockDbSet(users);
//             var mockRolesSet = CreateMockDbSet(roles);
//
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//             _mockContext.Setup(c => c.Roles).Returns(mockRolesSet.Object);
//
//             _mockContext.SetupSequence(c => c.Users.AnyAsync(
//                 It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false)
//                 .ReturnsAsync(false);
//
//             _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<string>()))
//                 .Returns(("hash", "salt"));
//
//             _mockTokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("access_token");
//
//             _mockTokenService.Setup(t => t.GenerateRefreshToken())
//                 .Returns(("refresh_token", "refresh_token_hash"));
//
//             _mockTokenService.Setup(t => t.CreateUserSessionAsync(
//                 It.IsAny<Guid>(),
//                 It.IsAny<string>(),
//                 It.IsAny<DateTime>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new UserSession());
//
//             _mockEmailService.Setup(e => e.SendEmailConfirmationAsync(
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>()))
//                 .ThrowsAsync(new Exception("SMTP server unavailable"));
//
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result); // Регистрация должна пройти успешно даже при ошибке отправки email
//             _mockLogger.Verify(
//                 x => x.Log(
//                     LogLevel.Error,
//                     It.IsAny<EventId>(),
//                     It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send confirmation email")),
//                     It.IsAny<Exception>(),
//                     It.IsAny<Func<It.IsAnyType, Exception, string>>()),
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task Handle_MinimalData_RegistersSuccessfully()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "minimal@example.com",
//                 username: "minimaluser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!");
//
//             var users = new List<User>();
//             var roles = new List<Role>
//             {
//                 new Role { Id = Guid.NewGuid(), Name = "User" }
//             };
//
//             var mockUsersSet = CreateMockDbSet(users);
//             var mockRolesSet = CreateMockDbSet(roles);
//
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//             _mockContext.Setup(c => c.Roles).Returns(mockRolesSet.Object);
//
//             _mockContext.SetupSequence(c => c.Users.AnyAsync(
//                 It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false)
//                 .ReturnsAsync(false);
//
//             _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<string>()))
//                 .Returns(("hash", "salt"));
//
//             _mockTokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("access_token");
//
//             _mockTokenService.Setup(t => t.GenerateRefreshToken())
//                 .Returns(("refresh_token", "refresh_token_hash"));
//
//             _mockTokenService.Setup(t => t.CreateUserSessionAsync(
//                 It.IsAny<Guid>(),
//                 It.IsAny<string>(),
//                 It.IsAny<DateTime>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new UserSession());
//
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Null(result.User.FirstName);
//             Assert.Null(result.User.LastName);
//             Assert.Null(result.User.DateOfBirth);
//             Assert.Null(result.User.PhoneNumber);
//             Assert.False(result.User.IsEmailConfirmed);
//         }
//
//         [Fact]
//         public async Task Handle_UserPropertiesSetCorrectly()
//         {
//             // Arrange
//             var command = new RegisterCommand(
//                 email: "test@example.com",
//                 username: "testuser",
//                 password: "Password123!",
//                 confirmPassword: "Password123!",
//                 firstName: "Test",
//                 lastName: "User",
//                 dateOfBirth: new DateTime(2000, 1, 1),
//                 phoneNumber: "+1234567890");
//
//             var users = new List<User>();
//             var roles = new List<Role>
//             {
//                 new Role { Id = Guid.NewGuid(), Name = "User" }
//             };
//
//             var mockUsersSet = CreateMockDbSet(users);
//             var mockRolesSet = CreateMockDbSet(roles);
//
//             _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
//             _mockContext.Setup(c => c.Roles).Returns(mockRolesSet.Object);
//
//             _mockContext.SetupSequence(c => c.Users.AnyAsync(
//                 It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(false)
//                 .ReturnsAsync(false);
//
//             _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<string>()))
//                 .Returns(("hashed_password", "password_salt"));
//
//             _mockTokenService.Setup(t => t.GenerateAccessTokenAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .ReturnsAsync("access_token");
//
//             _mockTokenService.Setup(t => t.GenerateRefreshToken())
//                 .Returns(("refresh_token", "refresh_token_hash"));
//
//             _mockTokenService.Setup(t => t.CreateUserSessionAsync(
//                 It.IsAny<Guid>(),
//                 It.IsAny<string>(),
//                 It.IsAny<DateTime>(),
//                 It.IsAny<string>(),
//                 It.IsAny<string>(),
//                 It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(new UserSession());
//
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .Callback(() =>
//                 {
//                     // Проверяем, что пользователь добавлен с правильными свойствами
//                     var user = users.First();
//                     Assert.Equal("test@example.com", user.Email);
//                     Assert.Equal("testuser", user.Username);
//                     Assert.Equal("hashed_password", user.PasswordHash);
//                     Assert.Equal("password_salt", user.PasswordSalt);
//                     Assert.Equal("Test", user.FirstName);
//                     Assert.Equal("User", user.LastName);
//                     Assert.Equal(new DateTime(2000, 1, 1), user.DateOfBirth);
//                     Assert.Equal("+1234567890", user.PhoneNumber);
//                     Assert.False(user.IsEmailConfirmed);
//                     Assert.True(user.IsActive);
//                     Assert.NotNull(user.LastLoginAt);
//                 })
//                 .ReturnsAsync(1);
//
//             // Act
//             await _handler.Handle(command, CancellationToken.None);
//         }
//     }
// }
