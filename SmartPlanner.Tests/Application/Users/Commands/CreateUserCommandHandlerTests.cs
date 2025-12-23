// // Tests/Application/Users/Commands/CreateUserCommandHandlerTests.cs
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Moq;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Application.Users.Commands;
// using SmartPlanner.Application.Users.Dtos;
// using SmartPlanner.Domain.Entities;
// using Xunit;
//
// namespace SmartPlanner.Application.UnitTests.Users.Commands;
//
// public class CreateUserCommandHandlerTests
// {
//     private readonly Mock<IApplicationDbContext> _mockContext;
//     private readonly Mock<DbSet<User>> _mockUsersSet;
//     private readonly Mock<DbSet<Interest>> _mockInterestsSet;
//     private readonly Mock<DbSet<UserInterest>> _mockUserInterestsSet;
//     private readonly CreateUserCommandHandler _handler;
//
//     public CreateUserCommandHandlerTests()
//     {
//         _mockContext = new Mock<IApplicationDbContext>();
//         _mockUsersSet = new Mock<DbSet<User>>();
//         _mockInterestsSet = new Mock<DbSet<Interest>>();
//         _mockUserInterestsSet = new Mock<DbSet<UserInterest>>();
//
//         _mockContext.Setup(c => c.Users).Returns(_mockUsersSet.Object);
//         _mockContext.Setup(c => c.Interests).Returns(_mockInterestsSet.Object);
//         _mockContext.Setup(c => c.UserInterests).Returns(_mockUserInterestsSet.Object);
//
//         _handler = new CreateUserCommandHandler(_mockContext.Object);
//     }
//
//     [Fact]
//     public async Task Handle_UserWithEmailAlreadyExists_ReturnsExistingUser()
//     {
//         // Arrange
//         var existingUser = new User
//         {
//             Id = Guid.NewGuid(),
//             Username = "existinguser",
//             Email = "test@example.com",
//             PasswordHash = "hashed",
//             Balance = 0,
//             StreakCount = 0,
//             LastLoginAt = DateTime.UtcNow,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//
//         var queryableUsers = new List<User> { existingUser }.AsQueryable();
//         var mockDbSet = new Mock<DbSet<User>>();
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(queryableUsers.Provider);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryableUsers.Expression);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryableUsers.ElementType);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => queryableUsers.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//
//         var command = new CreateUserCommand
//         {
//             Username = "newuser",
//             Email = "test@example.com",
//             Password = "Password123",
//             Interests = new List<string>()
//         };
//
//         // Act
//         var result = await _handler.Handle(command, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();
//         result.Email.Should().Be("test@example.com");
//         _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
//     }
//
//     [Fact]
//     public async Task Handle_UserWithUsernameAlreadyExists_ReturnsExistingUser()
//     {
//         // Arrange
//         var existingUser = new User
//         {
//             Id = Guid.NewGuid(),
//             Username = "existinguser",
//             Email = "old@example.com",
//             PasswordHash = "hashed",
//             Balance = 0,
//             StreakCount = 0,
//             LastLoginAt = DateTime.UtcNow,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };
//
//         var queryableUsers = new List<User> { existingUser }.AsQueryable();
//         var mockDbSet = new Mock<DbSet<User>>();
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(queryableUsers.Provider);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryableUsers.Expression);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryableUsers.ElementType);
//         mockDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => queryableUsers.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);
//
//         var command = new CreateUserCommand
//         {
//             Username = "existinguser",
//             Email = "new@example.com",
//             Password = "Password123",
//             Interests = new List<string>()
//         };
//
//         // Act
//         var result = await _handler.Handle(command, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();
//         result.Username.Should().Be("existinguser");
//         _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
//     }
//
//     [Fact]
//     public async Task Handle_NewUserWithoutInterests_CreatesUserSuccessfully()
//     {
//         // Arrange
//         var emptyUsersList = new List<User>().AsQueryable();
//         var mockUsersDbSet = new Mock<DbSet<User>>();
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(emptyUsersList.Provider);
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(emptyUsersList.Expression);
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(emptyUsersList.ElementType);
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => emptyUsersList.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);
//         _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
//
//         var command = new CreateUserCommand
//         {
//             Username = "newuser",
//             Email = "new@example.com",
//             Password = "Password123",
//             Interests = new List<string>()
//         };
//
//         // Setup для добавления пользователя
//         User capturedUser = null;
//         _mockUsersSet.Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//             .Callback<User, CancellationToken>((user, _) => capturedUser = user)
//             .Returns((User user, CancellationToken token) =>
//                 ValueTask.FromResult(Mock.Of<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<User>>()));
//
//         // Act
//         var result = await _handler.Handle(command, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();
//         result.Username.Should().Be("newuser");
//         result.Email.Should().Be("new@example.com");
//         capturedUser.Should().NotBeNull();
//         capturedUser!.Username.Should().Be("newuser");
//         capturedUser.PasswordHash.Should().NotBeNullOrEmpty();
//         capturedUser.PasswordHash.Should().NotBe("Password123"); // Should be hashed
//         _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//     }
//
//     [Fact]
//     public async Task Handle_NewUserWithNewInterests_CreatesUserWithInterests()
//     {
//         // Arrange
//         var emptyUsersList = new List<User>().AsQueryable();
//         var mockUsersDbSet = new Mock<DbSet<User>>();
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(emptyUsersList.Provider);
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(emptyUsersList.Expression);
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(emptyUsersList.ElementType);
//         mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => emptyUsersList.GetEnumerator());
//
//         var emptyInterestsList = new List<Interest>().AsQueryable();
//         var mockInterestsDbSet = new Mock<DbSet<Interest>>();
//         mockInterestsDbSet.As<IQueryable<Interest>>().Setup(m => m.Provider).Returns(emptyInterestsList.Provider);
//         mockInterestsDbSet.As<IQueryable<Interest>>().Setup(m => m.Expression).Returns(emptyInterestsList.Expression);
//         mockInterestsDbSet.As<IQueryable<Interest>>().Setup(m => m.ElementType).Returns(emptyInterestsList.ElementType);
//         mockInterestsDbSet.As<IQueryable<Interest>>().Setup(m => m.GetEnumerator()).Returns(() => emptyInterestsList.GetEnumerator());
//
//         _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);
//         _mockContext.Setup(c => c.Interests).Returns(mockInterestsDbSet.Object);
//         _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
//
//         var command = new CreateUserCommand
//         {
//             Username = "newuser",
//             Email = "new@example.com",
//             Password = "Password123",
//             Interests = new List<string> { "Programming", "Music" }
//         };
//
//         var capturedInterests = new List<Interest>();
//         var capturedUserInterests = new List<UserInterest>();
//
//         _mockInterestsSet.Setup(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()))
//             .Callback<Interest, CancellationToken>((interest, _) => capturedInterests.Add(interest))
//             .Returns(Task.FromResult((object)null));
//
//         _mockUserInterestsSet.Setup(s => s.AddAsync(It.IsAny<UserInterest>(), It.IsAny<CancellationToken>()))
//             .Callback<UserInterest, CancellationToken>((userInterest, _) => capturedUserInterests.Add(userInterest))
//             .Returns(Task.FromResult((object)null));
//
//         // Act
//         var result = await _handler.Handle(command, CancellationToken.None);
//
//         // Assert
//         result.Should().NotBeNull();
//         capturedInterests.Should().HaveCount(2);
//         capturedInterests[0].Name.Should().Be("Programming");
//         capturedInterests[1].Name.Should().Be("Music");
//         capturedUserInterests.Should().HaveCount(2);
//         _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//     }
// }
