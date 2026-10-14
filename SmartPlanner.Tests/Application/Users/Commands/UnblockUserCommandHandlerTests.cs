// // Tests/Application.UnitTests/Users/Commands/UnblockUserCommandHandlerTests.cs
//
// using FluentAssertions;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using Moq;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Application.Users.Commands;
// using SmartPlanner.Domain.Entities;
// using Xunit;
//
// namespace SmartPlanner.Application.UnitTests.Users.Commands
// {
//     public class UnblockUserCommandHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _mockContext;
//         private readonly UnblockUserCommandHandler _handler;
//         private readonly Guid _adminUserId = Guid.NewGuid();
//         private readonly Guid _blockedUserId = Guid.NewGuid();
//         private readonly User _blockedUser;
//
//         public UnblockUserCommandHandlerTests()
//         {
//             _mockContext = new Mock<IApplicationDbContext>();
//             _handler = new UnblockUserCommandHandler(_mockContext.Object);
//
//             // Подготовка тестового заблокированного пользователя
//             _blockedUser = new User
//             {
//                 Id = _blockedUserId,
//                 Email = "blocked@test.com",
//                 Username = "blockeduser",
//                 IsActive = false, // Заблокирован
//                 IsDeleted = false,
//                 BlockedAt = DateTime.UtcNow.AddDays(-1),
//                 BlockedBy = Guid.NewGuid(),
//                 BlockReason = "Multiple failed login attempts",
//                 CreatedAt = DateTime.UtcNow.AddDays(-30),
//                 UpdatedAt = DateTime.UtcNow.AddDays(-1)
//             };
//         }
//
//         private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
//         {
//             var mockSet = new Mock<DbSet<T>>();
//             mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
//             mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
//             mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
//             mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
//             mockSet.As<IAsyncEnumerable<T>>()
//                 .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
//                 .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
//             mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
//             return mockSet;
//         }
//
//         [Fact]
//         public async Task Handle_ShouldUnblockUser_WhenUserExistsAndBlocked()
//         {
//             // Arrange
//             var users = new List<User> { _blockedUser }.AsQueryable();
//             var mockDbSet = CreateMockDbSet(users);
//
//             _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             var command = new UnblockUserCommand
//             {
//                 UserId = _blockedUserId,
//                 UnblockedBy = _adminUserId
//             };
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             result.Should().Be(Unit.Value);
//             _blockedUser.IsActive.Should().BeTrue();
//             _blockedUser.BlockedAt.Should().BeNull();
//             _blockedUser.BlockedBy.Should().BeNull();
//             _blockedUser.BlockReason.Should().BeNull();
//             _blockedUser.UnblockedAt.Should().NotBeNull();
//             _blockedUser.UnblockedBy.Should().Be(_adminUserId);
//             _blockedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
//
//             _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task Handle_ShouldDoNothing_WhenUserAlreadyActive()
//         {
//             // Arrange
//             var activeUser = new User
//             {
//                 Id = _blockedUserId,
//                 Email = "active@test.com",
//                 Username = "activeuser",
//                 IsActive = true, // Уже активен
//                 IsDeleted = false,
//                 CreatedAt = DateTime.UtcNow,
//                 UpdatedAt = DateTime.UtcNow
//             };
//
//             var users = new List<User> { activeUser }.AsQueryable();
//             var mockDbSet = CreateMockDbSet(users);
//
//             _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(0);
//
//             var command = new UnblockUserCommand
//             {
//                 UserId = _blockedUserId,
//                 UnblockedBy = _adminUserId
//             };
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             result.Should().Be(Unit.Value);
//             activeUser.IsActive.Should().BeTrue(); // Остался активным
//             activeUser.UnblockedAt.Should().BeNull(); // Не был разблокирован
//             activeUser.UnblockedBy.Should().BeNull();
//
//             _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task Handle_ShouldThrowException_WhenUserNotFound()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             var mockDbSet = CreateMockDbSet(users);
//
//             _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//
//             var command = new UnblockUserCommand
//             {
//                 UserId = Guid.NewGuid(), // Несуществующий пользователь
//                 UnblockedBy = _adminUserId
//             };
//
//             // Act & Assert
//             await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                 _handler.Handle(command, CancellationToken.None));
//
//             _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task Handle_ShouldClearSecurityFlags_WhenUnblockingUser()
//         {
//             // Arrange
//             var userWithSecurityIssues = new User
//             {
//                 Id = _blockedUserId,
//                 Email = "security@test.com",
//                 Username = "securityuser",
//                 IsActive = false,
//                 IsDeleted = false,
//                 FailedLoginAttempts = 5,
//                 LockoutEnd = DateTime.UtcNow.AddHours(1),
//                 BlockedAt = DateTime.UtcNow.AddHours(-2),
//                 BlockedBy = Guid.NewGuid(),
//                 BlockReason = "Security violation",
//                 CreatedAt = DateTime.UtcNow,
//                 UpdatedAt = DateTime.UtcNow
//             };
//
//             var users = new List<User> { userWithSecurityIssues }.AsQueryable();
//             var mockDbSet = CreateMockDbSet(users);
//
//             _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             var command = new UnblockUserCommand
//             {
//                 UserId = _blockedUserId,
//                 UnblockedBy = _adminUserId
//             };
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             result.Should().Be(Unit.Value);
//             userWithSecurityIssues.IsActive.Should().BeTrue();
//             userWithSecurityIssues.FailedLoginAttempts.Should().Be(0); // Сбрасываем счетчик
//             userWithSecurityIssues.LockoutEnd.Should().BeNull(); // Снимаем блокировку
//             userWithSecurityIssues.BlockedAt.Should().BeNull();
//             userWithSecurityIssues.BlockedBy.Should().BeNull();
//             userWithSecurityIssues.BlockReason.Should().BeNull();
//             userWithSecurityIssues.UnblockedAt.Should().NotBeNull();
//             userWithSecurityIssues.UnblockedBy.Should().Be(_adminUserId);
//         }
//
//         [Fact]
//         public async Task Handle_ShouldNotUnblockDeletedUser()
//         {
//             // Arrange
//             var deletedUser = new User
//             {
//                 Id = _blockedUserId,
//                 Email = "deleted@test.com",
//                 Username = "deleteduser",
//                 IsActive = false,
//                 IsDeleted = true, // Удаленный пользователь
//                 DeletedAt = DateTime.UtcNow.AddDays(-1),
//                 BlockedAt = DateTime.UtcNow.AddDays(-2),
//                 BlockedBy = Guid.NewGuid(),
//                 CreatedAt = DateTime.UtcNow.AddDays(-30),
//                 UpdatedAt = DateTime.UtcNow.AddDays(-2)
//             };
//
//             var users = new List<User> { deletedUser }.AsQueryable();
//             var mockDbSet = CreateMockDbSet(users);
//
//             _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//
//             var command = new UnblockUserCommand
//             {
//                 UserId = _blockedUserId,
//                 UnblockedBy = _adminUserId
//             };
//
//             // Act & Assert
//             await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                 _handler.Handle(command, CancellationToken.None));
//
//             deletedUser.IsActive.Should().BeFalse(); // Остался неактивным
//             deletedUser.IsDeleted.Should().BeTrue(); // Остался удаленным
//             deletedUser.UnblockedAt.Should().BeNull(); // Не был разблокирован
//         }
//     }
// }
