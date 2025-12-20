using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Data;
using SmartPlanner.Tests.TestHelpers;
using Xunit;
using Xunit.Sdk;
using Microsoft.EntityFrameworkCore.Storage;

namespace SmartPlanner.Tests.Application.Achievements
{
    public class AwardAchievementCommandHandlerTests : IDisposable
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly AwardAchievementCommandHandler _handler;
        private readonly List<User> _users;
        private readonly List<Achievement> _achievements;
        private readonly List<UserAchievement> _userAchievements;
        private readonly Mock<IDbContextTransaction> _mockTransaction;

        public AwardAchievementCommandHandlerTests()
        {
            _users = new List<User>();
            _achievements = new List<Achievement>();
            _userAchievements = new List<UserAchievement>();

            var mockUsers = CreateMockDbSet(_users);
            var mockAchievements = CreateMockDbSet(_achievements);
            var mockUserAchievements = CreateMockDbSet(_userAchievements);

            _mockContext = new Mock<IApplicationDbContext>();
            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);
            
            _mockContext
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockContext
                .Setup(c => c.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockTransaction.Object);

            _handler = new AwardAchievementCommandHandler(_mockContext.Object);
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();
            
            var asyncQueryProvider = new TestAsyncQueryProvider<T>(queryable.Provider);
            
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));
                
            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(asyncQueryProvider);
                
            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);
                
            mockSet.As<IQueryable<T>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);
                
            mockSet.As<IQueryable<T>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => queryable.GetEnumerator());
                
            // Setup for FirstOrDefaultAsync
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));
                
            // Setup for AnyAsync
            mockSet.As<IQueryable<T>>()
                .Setup(x => x.Provider)
                .Returns(asyncQueryProvider);
                
            return mockSet;
        }

        public void Dispose()
        {
            // Clean up test data
            _users.Clear();
            _achievements.Clear();
            _userAchievements.Clear();
        }


        [Fact]
        public async Task Handle_AchievementAndUserExist_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var achievementId = Guid.NewGuid();

            var user = new User { Id = userId, Username = "testuser", Balance = 0 };
            var achievement = new Achievement
            {
                Id = achievementId,
                Name = "Test Achievement",
                Type = AchievementType.Streak,
                Condition = "streak:7",
                RewardAmount = 100
            };

            _users.Add(user);
            _achievements.Add(achievement);

            var command = new AwardAchievementCommand
            {
                UserId = userId,
                AchievementId = achievementId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Single(_userAchievements);
            Assert.Equal(100, user.Balance);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AchievementAlreadyAwarded_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var achievementId = Guid.NewGuid();

            var user = new User { Id = userId, Username = "testuser", Balance = 0 };
            var achievement = new Achievement
            {
                Id = achievementId,
                Name = "Test Achievement",
                Type = AchievementType.Streak,
                Condition = "streak:7",
                RewardAmount = 100
            };

            var userAchievement = new UserAchievement
            {
                UserId = userId,
                AchievementId = achievementId,
                AwardedAt = DateTime.UtcNow
            };

            _users.Add(user);
            _achievements.Add(achievement);
            _userAchievements.Add(userAchievement);

            var command = new AwardAchievementCommand
            {
                UserId = userId,
                AchievementId = achievementId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            Assert.Equal(0, user.Balance);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AchievementNotFound_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var achievementId = Guid.NewGuid();

            var user = new User { Id = userId, Username = "testuser", Balance = 0 };
            _users.Add(user);

            var command = new AwardAchievementCommand
            {
                UserId = userId,
                AchievementId = achievementId // This achievement doesn't exist
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            Assert.Empty(_userAchievements);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var achievementId = Guid.NewGuid();

            // Don't add any users to the mock DbSet
            _users.Clear();

            var command = new AwardAchievementCommand
            {
                UserId = userId, // This user doesn't exist
                AchievementId = achievementId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            Assert.Empty(_userAchievements);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
