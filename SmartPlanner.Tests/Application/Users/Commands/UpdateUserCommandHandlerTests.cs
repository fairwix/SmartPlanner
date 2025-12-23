using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Users.Commands;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Users.Commands
{
    public class UpdateUserCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<DbSet<User>> _mockUsersSet;
        private readonly Mock<DbSet<Interest>> _mockInterestsSet;
        private readonly Mock<DbSet<UserInterest>> _mockUserInterestsSet;
        private readonly UpdateUserCommandHandler _handler;
        private readonly List<User> _users;
        private readonly List<Interest> _interests;
        private readonly List<UserInterest> _userInterests;

        public UpdateUserCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockUsersSet = new Mock<DbSet<User>>();
            _mockInterestsSet = new Mock<DbSet<Interest>>();
            _mockUserInterestsSet = new Mock<DbSet<UserInterest>>();

            _users = new List<User>();
            _interests = new List<Interest>();
            _userInterests = new List<UserInterest>();

            // Настройка моков для User
            SetupMockDbSet(_mockUsersSet, _users);
            _mockContext.Setup(c => c.Users).Returns(_mockUsersSet.Object);

            // Настройка моков для Interest
            SetupMockDbSet(_mockInterestsSet, _interests);
            _mockContext.Setup(c => c.Interests).Returns(_mockInterestsSet.Object);

            // Настройка моков для UserInterest
            SetupMockDbSet(_mockUserInterestsSet, _userInterests);
            _mockContext.Setup(c => c.UserInterests).Returns(_mockUserInterestsSet.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _handler = new UpdateUserCommandHandler(_mockContext.Object);
        }

        private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            // Исправленный метод AddAsync
            mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((T entity, CancellationToken token) =>
                {
                    data.Add(entity);

                    // Создаем мок EntityEntry<T>
                    var mockEntry = new Mock<EntityEntry<T>>();
                    var mockInternalEntry = new Mock<InternalEntityEntry>();

                    // Возвращаем мок EntityEntry
                    return mockEntry.Object;
                });

            mockSet.Setup(m => m.RemoveRange(It.IsAny<IEnumerable<T>>()))
                .Callback<IEnumerable<T>>(entities =>
                {
                    foreach (var entity in entities.ToList())
                    {
                        data.Remove(entity);
                    }
                });
        }

        [Fact]
        public async Task Handle_UserNotFound_ShouldReturnNull()
        {
            // Arrange
            var command = new UpdateUserCommand { UserId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_UpdateUsername_WhenUsernameNotTaken_ShouldUpdateSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = userId,
                Username = "oldusername",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Username = "newusername"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("newusername");
            existingUser.Username.Should().Be("newusername");
            existingUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateUsername_WhenUsernameTaken_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var existingUser = new User
            {
                Id = userId,
                Username = "oldusername",
                Email = "test1@example.com"
            };

            var otherUser = new User
            {
                Id = otherUserId,
                Username = "takenusername",
                Email = "test2@example.com"
            };

            _users.Add(existingUser);
            _users.Add(otherUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Username = "takenusername" // Этот username уже занят
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UpdateUsername_WhenSameUsername_ShouldNotUpdate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = userId,
                Username = "existingusername",
                Email = "test@example.com"
            };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Username = "existingusername" // Тот же самый username
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            existingUser.Username.Should().Be("existingusername");
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AddNewInterests_ShouldCreateInterestsAndAssociations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };
            _users.Add(existingUser);

            // Добавляем существующие интересы пользователя
            var existingInterest = new Interest
            {
                Id = Guid.NewGuid(),
                Name = "ExistingInterest"
            };
            _interests.Add(existingInterest);

            var existingUserInterest = new UserInterest
            {
                UserId = userId,
                InterestId = existingInterest.Id,
                User = existingUser,
                Interest = existingInterest
            };
            _userInterests.Add(existingUserInterest);

            existingUser.UserInterests = new List<UserInterest> { existingUserInterest };

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Interests = new List<string> { "NewInterest1", "NewInterest2" }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _mockUserInterestsSet.Verify(s => s.RemoveRange(It.IsAny<IEnumerable<UserInterest>>()), Times.Once);
            _mockInterestsSet.Verify(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockUserInterestsSet.Verify(s => s.AddAsync(It.IsAny<UserInterest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task Handle_UpdateInterests_WithSameInterests_ShouldNotMakeChanges()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var interest1 = new Interest { Id = Guid.NewGuid(), Name = "Programming" };
            var interest2 = new Interest { Id = Guid.NewGuid(), Name = "Fitness" };

            _interests.Add(interest1);
            _interests.Add(interest2);

            var existingUser = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };

            var userInterest1 = new UserInterest
            {
                UserId = userId,
                InterestId = interest1.Id,
                User = existingUser,
                Interest = interest1
            };

            var userInterest2 = new UserInterest
            {
                UserId = userId,
                InterestId = interest2.Id,
                User = existingUser,
                Interest = interest2
            };

            _userInterests.Add(userInterest1);
            _userInterests.Add(userInterest2);

            existingUser.UserInterests = new List<UserInterest> { userInterest1, userInterest2 };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Interests = new List<string> { "Programming", "Fitness" } // Те же самые интересы
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _mockUserInterestsSet.Verify(s => s.RemoveRange(It.IsAny<IEnumerable<UserInterest>>()), Times.Never);
            _mockInterestsSet.Verify(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UpdateInterests_WithEmptyList_ShouldRemoveAllInterests()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var interest = new Interest { Id = Guid.NewGuid(), Name = "Programming" };
            _interests.Add(interest);

            var existingUser = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };

            var userInterest = new UserInterest
            {
                UserId = userId,
                InterestId = interest.Id,
                User = existingUser,
                Interest = interest
            };

            _userInterests.Add(userInterest);
            existingUser.UserInterests = new List<UserInterest> { userInterest };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Interests = new List<string>() // Пустой список
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _mockUserInterestsSet.Verify(s => s.RemoveRange(It.IsAny<IEnumerable<UserInterest>>()), Times.Once);
            _mockInterestsSet.Verify(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_UpdateInterests_WithNull_ShouldNotChangeInterests()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var interest = new Interest { Id = Guid.NewGuid(), Name = "Programming" };
            _interests.Add(interest);

            var existingUser = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };

            var userInterest = new UserInterest
            {
                UserId = userId,
                InterestId = interest.Id,
                User = existingUser,
                Interest = interest
            };

            _userInterests.Add(userInterest);
            existingUser.UserInterests = new List<UserInterest> { userInterest };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Interests = null // null вместо списка
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _mockUserInterestsSet.Verify(s => s.RemoveRange(It.IsAny<IEnumerable<UserInterest>>()), Times.Never);
            _mockInterestsSet.Verify(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UpdateInterests_WithDuplicateNames_ShouldRemoveDuplicates()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Interests = new List<string> { "Programming", "programming", "PROGRAMMING" } // Дубликаты
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            // Должен быть создан только один интерес, так как дубликаты удаляются
            _mockInterestsSet.Verify(s => s.AddAsync(It.Is<Interest>(i => i.Name == "Programming"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateInterests_WithEmptyOrWhiteSpaceStrings_ShouldBeIgnored()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Interests = new List<string> { "", "  ", null, "ValidInterest" }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            // Должен быть создан только один валидный интерес
            _mockInterestsSet.Verify(s => s.AddAsync(It.Is<Interest>(i => i.Name == "ValidInterest"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateBothUsernameAndInterests_ShouldUpdateBoth()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                Id = userId,
                Username = "oldusername",
                Email = "test@example.com"
            };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId,
                Username = "newusername",
                Interests = new List<string> { "Programming", "Fitness" }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            existingUser.Username.Should().Be("newusername");
            _mockInterestsSet.Verify(s => s.AddAsync(It.IsAny<Interest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task Handle_MapToDto_ShouldReturnCorrectDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var interest = new Interest { Id = Guid.NewGuid(), Name = "Programming" };
            _interests.Add(interest);

            var existingUser = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow,
                Balance = 100,
                StreakCount = 5,
                LastLoginAt = DateTime.UtcNow.AddHours(-2)
            };

            var userInterest = new UserInterest
            {
                UserId = userId,
                InterestId = interest.Id,
                User = existingUser,
                Interest = interest
            };

            _userInterests.Add(userInterest);
            existingUser.UserInterests = new List<UserInterest> { userInterest };
            _users.Add(existingUser);

            var command = new UpdateUserCommand
            {
                UserId = userId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userId);
            result.Username.Should().Be("testuser");
            result.Email.Should().Be("test@example.com");
            result.Interests.Should().ContainSingle("Programming");
            result.Balance.Should().Be(100);
            result.StreakCount.Should().Be(5);

        }
    }
}
