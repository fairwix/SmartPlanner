// using System.Linq.Expressions;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Query;
// using Moq;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Application.Users.Commands;
// using SmartPlanner.Application.Users.Dtos;
// using SmartPlanner.Domain.Entities;
// using Xunit;
//
// namespace SmartPlanner.Application.Tests.Users.Commands
// {
//     public class CreateUserCommandHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _mockContext;
//         private readonly CreateUserCommandHandler _handler;
//         private readonly Mock<DbSet<User>> _mockUsersSet;
//         private readonly Mock<DbSet<Interest>> _mockInterestsSet;
//         private readonly Mock<DbSet<UserInterest>> _mockUserInterestsSet;
//
//         public CreateUserCommandHandlerTests()
//         {
//             _mockContext = new Mock<IApplicationDbContext>();
//             _handler = new CreateUserCommandHandler(_mockContext.Object);
//
//             _mockUsersSet = new Mock<DbSet<User>>();
//             _mockInterestsSet = new Mock<DbSet<Interest>>();
//             _mockUserInterestsSet = new Mock<DbSet<UserInterest>>();
//
//             _mockContext.Setup(c => c.Users).Returns(_mockUsersSet.Object);
//             _mockContext.Setup(c => c.Interests).Returns(_mockInterestsSet.Object);
//             _mockContext.Setup(c => c.UserInterests).Returns(_mockUserInterestsSet.Object);
//         }
//
//         [Fact]
//         public async Task Handle_UserWithEmailExists_ReturnsExistingUserDto()
//         {
//             // Arrange
//             var existingUser = new User
//             {
//                 Id = Guid.NewGuid(),
//                 Username = "existinguser",
//                 Email = "existing@example.com",
//                 PasswordHash = "hashedpassword",
//                 Balance = 100,
//                 StreakCount = 5,
//                 CreatedAt = DateTime.UtcNow.AddDays(-10),
//                 UpdatedAt = DateTime.UtcNow,
//                 UserInterests = new List<UserInterest>()
//             };
//
//             var users = new List<User> { existingUser }.AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             var command = new CreateUserCommand(
//                 Username: "newuser",
//                 Email: "existing@example.com", // Существующий email
//                 Password: "NewPassword123!",
//                 Interests: new List<string> { "Programming" });
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(existingUser.Id, result.Id);
//             Assert.Equal(existingUser.Username, result.Username);
//             Assert.Equal(existingUser.Email, result.Email);
//             Assert.Equal(existingUser.Balance, result.Balance);
//             Assert.Equal(existingUser.StreakCount, result.StreakCount);
//         }
//
//         [Fact]
//         public async Task Handle_UserWithUsernameExists_ReturnsExistingUserDto()
//         {
//             // Arrange
//             var existingUser = new User
//             {
//                 Id = Guid.NewGuid(),
//                 Username = "existinguser",
//                 Email = "user@example.com",
//                 PasswordHash = "hashedpassword",
//                 Balance = 50,
//                 StreakCount = 3,
//                 CreatedAt = DateTime.UtcNow.AddDays(-5),
//                 UpdatedAt = DateTime.UtcNow,
//                 UserInterests = new List<UserInterest>()
//             };
//
//             var users = new List<User> { existingUser }.AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             var command = new CreateUserCommand(
//                 Username: "existinguser", // Существующий username
//                 Email: "new@example.com",
//                 Password: "NewPassword123!",
//                 Interests: new List<string> { "Reading" });
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(existingUser.Id, result.Id);
//             Assert.Equal(existingUser.Username, result.Username);
//             Assert.Equal(existingUser.Email, result.Email);
//         }
//
//         [Fact]
//         public async Task Handle_NewUser_CreatesUserSuccessfully()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             var interests = new List<Interest>().AsQueryable();
//             SetupMockDbSet(_mockInterestsSet, interests);
//
//             var userInterests = new List<UserInterest>().AsQueryable();
//             SetupMockDbSet(_mockUserInterestsSet, userInterests);
//
//             var capturedUser = new List<User>();
//             _mockUsersSet.Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .Callback<User, CancellationToken>((user, _) => capturedUser.Add(user))
//                 .Returns(ValueTask.FromResult((object?)null));
//
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                 .ReturnsAsync(1);
//
//             var command = new CreateUserCommand(
//                 Username: "newuser",
//                 Email: "new@example.com",
//                 Password: "SecurePassword123!",
//                 Interests: new List<string> { "Programming", "Gaming" });
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal("newuser", result.Username);
//             Assert.Equal("new@example.com", result.Email);
//             Assert.Equal(0, result.Balance);
//             Assert.Equal(0, result.StreakCount);
//             Assert.NotNull(result.LastLogin);
//             Assert.NotNull(result.Interests);
//
//             _mockUsersSet.Verify(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
//             _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task Handle_NewUserWithInterests_CreatesInterests()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             var existingInterest = new Interest { Id = Guid.NewGuid(), Name = "Programming" };
//             var interests = new List<Interest> { existingInterest }.AsQueryable();
//             SetupMockDbSet(_mockInterestsSet, interests);
//
//             var userInterests = new List<UserInterest>().AsQueryable();
//             SetupMockDbSet(_mockUserInterestsSet, userInterests);
//
//             var capturedInterests = new List<Interest>();
//             _mockInterestsSet.Setup(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()))
//                 .Callback<Interest, CancellationToken>((interest, _) => capturedInterests.Add(interest))
//                 .Returns(ValueTask.FromResult((object?)null));
//
//             var command = new CreateUserCommand(
//                 Username: "newuser",
//                 Email: "new@example.com",
//                 Password: "password",
//                 Interests: new List<string> { "Programming", "NewInterest" });
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             _mockInterestsSet.Verify(s => s.AddAsync(It.Is<Interest>(i => i.Name == "NewInterest"), It.IsAny<CancellationToken>()), Times.Once);
//             _mockInterestsSet.Verify(s => s.AddAsync(It.Is<Interest>(i => i.Name == "Programming"), It.IsAny<CancellationToken>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task Handle_NewUserWithoutInterests_CreatesUserWithoutInterests()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             var command = new CreateUserCommand(
//                 Username: "newuser",
//                 Email: "new@example.com",
//                 Password: "password",
//                 Interests: null);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Empty(result.Interests);
//             _mockInterestsSet.Verify(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()), Times.Never);
//             _mockUserInterestsSet.Verify(s => s.AddAsync(It.IsAny<UserInterest>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task Handle_NewUserWithEmptyInterests_CreatesUserWithoutInterests()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             var command = new CreateUserCommand(
//                 Username: "newuser",
//                 Email: "new@example.com",
//                 Password: "password",
//                 Interests: new List<string>());
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Empty(result.Interests);
//             _mockInterestsSet.Verify(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()), Times.Never);
//             _mockUserInterestsSet.Verify(s => s.AddAsync(It.IsAny<UserInterest>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
//
//         [Fact]
//         public async Task Handle_PasswordIsHashed()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             User? capturedUser = null;
//             _mockUsersSet.Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .Callback<User, CancellationToken>((user, _) => capturedUser = user)
//                 .Returns(ValueTask.FromResult((object?)null));
//
//             var command = new CreateUserCommand(
//                 Username: "newuser",
//                 Email: "new@example.com",
//                 Password: "MyPassword123",
//                 Interests: null);
//
//             // Act
//             await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(capturedUser);
//             Assert.NotNull(capturedUser.PasswordHash);
//             Assert.NotEqual("MyPassword123", capturedUser.PasswordHash);
//             Assert.True(BCrypt.Net.BCrypt.Verify("MyPassword123", capturedUser.PasswordHash));
//         }
//
//         [Fact]
//         public async Task Handle_CancellationToken_IsRespected()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             var cancellationToken = new CancellationToken(canceled: true);
//             var command = new CreateUserCommand("test", "test@test.com", "password", null);
//
//             // Act & Assert
//             await Assert.ThrowsAsync<TaskCanceledException>(() =>
//                 _handler.Handle(command, cancellationToken));
//         }
//
//         [Fact]
//         public async Task Handle_NewUser_SetsDefaultValues()
//         {
//             // Arrange
//             var users = new List<User>().AsQueryable();
//             SetupMockDbSet(_mockUsersSet, users);
//
//             User? capturedUser = null;
//             _mockUsersSet.Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
//                 .Callback<User, CancellationToken>((user, _) => capturedUser = user)
//                 .Returns(ValueTask.FromResult((object?)null));
//
//             var command = new CreateUserCommand(
//                 Username: "newuser",
//                 Email: "new@example.com",
//                 Password: "password",
//                 Interests: null);
//
//             // Act
//             await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(capturedUser);
//             Assert.Equal(0, capturedUser.Balance);
//             Assert.Equal(0, capturedUser.StreakCount);
//             Assert.NotNull(capturedUser.LastLoginAt);
//             Assert.True(capturedUser.LastLoginAt.Value <= DateTime.UtcNow);
//             Assert.True(capturedUser.LastLoginAt.Value >= DateTime.UtcNow.AddSeconds(-5));
//         }
//
//         private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
//         {
//             mockSet.As<IAsyncEnumerable<T>>()
//                 .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
//                 .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
//
//             mockSet.As<IQueryable<T>>()
//                 .Setup(m => m.Provider)
//                 .Returns(new TestAsyncQueryProvider<T>(data.Provider));
//
//             mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
//             mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
//             mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
//         }
//     }
//
//     // Вспомогательные классы для async тестирования
//     internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
//     {
//         private readonly IEnumerator<T> _inner;
//         public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
//         public ValueTask DisposeAsync() => ValueTask.CompletedTask;
//         public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
//         public T Current => _inner.Current;
//     }
//
//     internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
//     {
//         private readonly IQueryProvider _inner;
//         public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
//         public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<T>(expression);
//         public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
//         public object Execute(Expression expression) => _inner.Execute(expression);
//         public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
//         public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
//         {
//             var result = Execute(expression);
//             return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
//                 .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0])
//                 .Invoke(null, new[] { result })!;
//         }
//     }
//
//     internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
//     {
//         public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
//         public TestAsyncEnumerable(Expression expression) : base(expression) { }
//         public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
//             => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
//     }
// }
