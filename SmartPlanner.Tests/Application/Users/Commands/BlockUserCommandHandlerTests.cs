// // Тесты будут требовать интерфейсы. Вот тесты для BlockUserCommandHandler:
// // Tests/Application/Users/Commands/BlockUserCommandHandlerTests.cs
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Moq;
// using SmartPlanner.Application.Auth.Interfaces;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Application.Security.Services;
// using SmartPlanner.Application.Users.Commands;
// using SmartPlanner.Domain.Entities;
// using Xunit;
//
// namespace SmartPlanner.Application.UnitTests.Users.Commands;
//
// public class BlockUserCommandHandlerTests
// {
//     private readonly Mock<IApplicationDbContext> _mockContext;
//     private readonly Mock<ITokenService> _mockTokenService;
//     private readonly Mock<IAuditService> _mockAuditService;
//     private readonly Mock<ILogger<BlockUserCommandHandler>> _mockLogger;
//     private readonly BlockUserCommandHandler _handler;
//
//     public BlockUserCommandHandlerTests()
//     {
//         _mockContext = new Mock<IApplicationDbContext>();
//         _mockTokenService = new Mock<ITokenService>();
//         _mockAuditService = new Mock<IAuditService>();
//         _mockLogger = new Mock<ILogger<BlockUserCommandHandler>>();
//
//         _handler = new BlockUserCommandHandler(
//             _mockContext.Object,
//             _mockTokenService.Object,
//             _mockAuditService.Object,
//             _mockLogger.Object);
//     }
//
//     [Fact]
//     public async Task Handle_UserExistsAndActive_BlocksUserAndRevokesSessions()
//     {
//         // Arrange
//         var userId = Guid.NewGuid();
//         var adminId = Guid.NewGuid();
//
//         var user = new User
//         {
//             Id = userId,
//             Username = "testuser",
//             Email = "test@example.com",
//             IsActive = true,
//             IsDeleted = false,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//
//         var users = new List<User> { user }.AsQueryable();
//         var mockDbSet = new Mock<DbSet<User>>();
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//         _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
//
//         _mockTokenService.Setup(s => s.RevokeUserSessionsAsync(userId, It.IsAny<CancellationToken>()))
//             .Returns(Task.CompletedTask);
//
//         _mockAuditService.Setup(s => s.LogSecurityEventAsync(
//             It.IsAny<SecurityEventType>(),
//             userId,
//             user.Email,
//             true,
//             It.IsAny<object>(),
//             It.IsAny<CancellationToken>()))
//             .Returns(Task.CompletedTask);
//
//         var command = new BlockUserCommand { UserId = userId, BlockedBy = adminId };
//
//         // Act
//         var result = await _handler.Handle(command, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();
//         user.IsActive.Should().BeFalse();
//         user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
//
//         _mockTokenService.Verify(s => s.RevokeUserSessionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
//         _mockAuditService.Verify(s => s.LogSecurityEventAsync(
//             SecurityEventType.UserBlocked,
//             userId,
//             user.Email,
//             true,
//             It.Is<object>(o => (o as dynamic).BlockedBy == adminId),
//             It.IsAny<CancellationToken>()), Times.Once);
//
//         _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//     }
//
//     [Fact]
//     public async Task Handle_UserAlreadyBlocked_LogsWarningAndReturns()
//     {
//         // Arrange
//         var userId = Guid.NewGuid();
//         var adminId = Guid.NewGuid();
//
//         var user = new User
//         {
//             Id = userId,
//             Username = "testuser",
//             Email = "test@example.com",
//             IsActive = false, // Already blocked
//             IsDeleted = false,
//             UpdatedAt = DateTime.UtcNow.AddDays(-1)
//         };
//
//         var users = new List<User> { user }.AsQueryable();
//         var mockDbSet = new Mock<DbSet<User>>();
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//
//         var command = new BlockUserCommand { UserId = userId, BlockedBy = adminId };
//
//         // Act
//         var result = await _handler.Handle(command, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();
//         user.IsActive.Should().BeFalse(); // Should remain false
//         _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
//         _mockTokenService.Verify(s => s.RevokeUserSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
//     }
//
//     [Fact]
//     public async Task Handle_UserNotFound_ThrowsArgumentException()
//     {
//         // Arrange
//         var userId = Guid.NewGuid();
//         var adminId = Guid.NewGuid();
//
//         var emptyUsersList = new List<User>().AsQueryable();
//         var mockDbSet = new Mock<DbSet<User>>();
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(emptyUsersList.Provider);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(emptyUsersList.Expression);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(emptyUsersList.ElementType);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => emptyUsersList.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//
//         var command = new BlockUserCommand { UserId = userId, BlockedBy = adminId };
//
//         // Act & Assert
//         await Assert.ThrowsAsync<ArgumentException>(() =>
//             _handler.Handle(command, CancellationToken.None));
//     }
//
//     [Fact]
//     public async Task Handle_UserDeleted_ThrowsArgumentException()
//     {
//         // Arrange
//         var userId = Guid.NewGuid();
//         var adminId = Guid.NewGuid();
//
//         var user = new User
//         {
//             Id = userId,
//             Username = "testuser",
//             Email = "test@example.com",
//             IsActive = true,
//             IsDeleted = true // Deleted user
//         };
//
//         var users = new List<User> { user }.AsQueryable();
//         var mockDbSet = new Mock<DbSet<User>>();
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//
//         var command = new BlockUserCommand { UserId = userId, BlockedBy = adminId };
//
//         // Act & Assert
//         await Assert.ThrowsAsync<ArgumentException>(() =>
//             _handler.Handle(command, CancellationToken.None));
//     }
// }
