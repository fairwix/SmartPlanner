using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Auth.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly Mock<ILogger<LoginCommandHandler>> _mockLogger;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockTokenService = new Mock<ITokenService>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockLogger = new Mock<ILogger<LoginCommandHandler>>();
            _mockAuditService = new Mock<IAuditService>();

            _handler = new LoginCommandHandler(
                _mockContext.Object,
                _mockTokenService.Object,
                _mockPasswordHasher.Object,
                _mockLogger.Object,
                _mockAuditService.Object
            );
        }

        // [Fact]
        // public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
        // {
        //     // Arrange
        //     var command = new LoginCommand
        //     {
        //         EmailOrUsername = "test@example.com",
        //         Password = "password123",
        //         DeviceInfo = "Test Device",
        //         IpAddress = "127.0.0.1"
        //     };
        //
        //     var mockUsersDbSet = new Mock<DbSet<User>>();
        //     var users = new List<User>().AsQueryable();
        //
        //     mockUsersDbSet.As<IAsyncEnumerable<User>>()
        //         .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
        //         .Returns(new TestAsyncEnumerator<User>(users.GetEnumerator()));
        //
        //     mockUsersDbSet.As<IQueryable<User>>()
        //         .Setup(m => m.Provider)
        //         .Returns(new TestAsyncQueryProvider<User>(users.Provider));
        //
        //     mockUsersDbSet.As<IQueryable<User>>()
        //         .Setup(m => m.Expression)
        //         .Returns(users.Expression);
        //
        //     mockUsersDbSet.As<IQueryable<User>>()
        //         .Setup(m => m.ElementType)
        //         .Returns(users.ElementType);
        //
        //     mockUsersDbSet.As<IQueryable<User>>()
        //         .Setup(m => m.GetEnumerator())
        //         .Returns(users.GetEnumerator());
        //
        //     _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);
        //
        //     // Используем конкретную перегрузку метода с помощью лямбда-выражения
        //     // Перегрузка, которая принимает string как первый параметр (email)
        //     _mockAuditService.Setup(x => x.LogSecurityEventAsync(
        //             SecurityEventType.FailedLogin,
        //             It.IsAny<string>(), // email - string
        //             It.IsAny<string>(), // ipAddress
        //             It.IsAny<string>(), // userAgent
        //             false, // success
        //             It.IsAny<object>(), // details
        //             It.IsAny<CancellationToken>()))
        //         .Returns(Task.CompletedTask);
        //
        //     // Act & Assert
        //     await Assert.ThrowsAsync<UnauthorizedAccessException>(
        //         () => _handler.Handle(command, CancellationToken.None));
        // }

        [Fact]
        public async Task Handle_InactiveUser_ThrowsUnauthorizedException()
        {
            // Arrange
            var command = new LoginCommand
            {
                EmailOrUsername = "test@example.com",
                Password = "password123",
                DeviceInfo = "Test Device",
                IpAddress = "127.0.0.1"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                IsActive = false,
                IsDeleted = false,
                IsEmailConfirmed = true,
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            var mockUsersDbSet = CreateMockDbSet(new List<User> { user });
            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            _mockAuditService.Setup(x => x.LogSecurityEventAsync(
                SecurityEventType.FailedLogin,
                It.IsAny<Guid>(), // userId (Guid)
                It.IsAny<string>(), // email (string)
                It.IsAny<string>(), // ipAddress (string)
                It.IsAny<string>(), // userAgent (string)
                false, // success (bool)
                It.IsAny<object>(), // details
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_InvalidPassword_ThrowsUnauthorizedException()
        {
            // Arrange
            var command = new LoginCommand
            {
                EmailOrUsername = "test@example.com",
                Password = "wrongpassword",
                DeviceInfo = "Test Device",
                IpAddress = "127.0.0.1"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                IsActive = true,
                IsDeleted = false,
                IsEmailConfirmed = true,
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            var mockUsersDbSet = CreateMockDbSet(new List<User> { user });
            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(false);

            _mockAuditService.Setup(x => x.LogSecurityEventAsync(
                SecurityEventType.FailedLogin,
                user.Id, // userId (Guid)
                user.Email, // email (string)
                command.IpAddress, // ipAddress (string)
                command.DeviceInfo, // userAgent (string)
                false, // success (bool)
                It.IsAny<object>(), // details
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var command = new LoginCommand
            {
                EmailOrUsername = "test@example.com",
                Password = "password123",
                DeviceInfo = "Test Device",
                IpAddress = "127.0.0.1"
            };

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = "User"
            };

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = "CanViewDashboard"
            };

            var rolePermission = new RolePermission
            {
                Role = role,
                Permission = permission
            };

            role.RolePermissions = new List<RolePermission> { rolePermission };

            var userRole = new UserRole
            {
                UserId = Guid.NewGuid(),
                Role = role
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "1234567890",
                IsActive = true,
                IsDeleted = false,
                IsEmailConfirmed = true,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LastLoginAt = null,
                UserRoles = new List<UserRole> { userRole }
            };

            var mockUsersDbSet = CreateMockDbSet(new List<User> { user });
            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(1));

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(true);

            _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("access-token");

            _mockTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns(("refresh-token", "refresh-token-hash"));

            _mockTokenService.Setup(x => x.CreateUserSessionAsync(
                user.Id,
                "refresh-token-hash",
                It.IsAny<DateTime>(),
                command.DeviceInfo,
                command.IpAddress,
                It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask); // Используем лямбду

            _mockAuditService.Setup(x => x.LogSecurityEventAsync(
                SecurityEventType.Login,
                user.Id, // userId (Guid)
                user.Email, // email (string)
                command.IpAddress, // ipAddress (string)
                command.DeviceInfo, // userAgent (string)
                true, // success (bool)
                It.IsAny<object>(), // details (может быть null)
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
            result.User.Id.Should().Be(user.Id);
            result.User.Email.Should().Be(user.Email);
            result.User.Username.Should().Be(user.Username);
            result.User.Roles.Should().Contain("User");
            result.User.Permissions.Should().Contain("CanViewDashboard");

            _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UnconfirmedEmail_ThrowsUnauthorizedException()
        {
            // Arrange
            var command = new LoginCommand
            {
                EmailOrUsername = "test@example.com",
                Password = "password123",
                DeviceInfo = "Test Device",
                IpAddress = "127.0.0.1"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                IsActive = true,
                IsDeleted = false,
                IsEmailConfirmed = false, // Email не подтвержден
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            var mockUsersDbSet = CreateMockDbSet(new List<User> { user });
            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            _mockPasswordHasher.Setup(x => x.VerifyPassword(
                command.Password, user.PasswordHash, user.PasswordSalt))
                .Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_DeletedUser_ThrowsUnauthorizedException()
        {
            // Arrange
            var command = new LoginCommand
            {
                EmailOrUsername = "test@example.com",
                Password = "password123",
                DeviceInfo = "Test Device",
                IpAddress = "127.0.0.1"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                IsActive = true,
                IsDeleted = true, // Пользователь удален
                IsEmailConfirmed = true,
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            var mockUsersDbSet = CreateMockDbSet(new List<User> { user });
            _mockContext.Setup(x => x.Users).Returns(mockUsersDbSet.Object);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        // Вспомогательный метод для создания мока DbSet
        private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => queryable.GetEnumerator());

            return mockSet;
        }
    }

    // Вспомогательные классы для асинхронных операций
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(
                    name: nameof(IQueryProvider.Execute),
                    genericParameterCount: 1,
                    types: new[] { typeof(Expression) })!
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))?
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult })!;
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
