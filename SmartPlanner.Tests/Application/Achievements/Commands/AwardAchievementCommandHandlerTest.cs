// SmartPlanner.Tests/Application/Achievements/Commands/AwardAchievementCommandHandlerTests.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Application.Achievements.Queries;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using Xunit;
using Xunit.Sdk;

namespace SmartPlanner.Tests.Application.Achievements.Commands
{
    public class AwardAchievementCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AwardAchievementCommandHandler>> _mockLogger;
        private readonly AwardAchievementCommandHandler _handler;
        private readonly Guid _testUserId;
        private readonly Guid _testAchievementId;

        public AwardAchievementCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<AwardAchievementCommandHandler>>();

            _testUserId = Guid.NewGuid();
            _testAchievementId = Guid.NewGuid();

            _handler = new AwardAchievementCommandHandler(_mockContext.Object);
        }

        private void SetupMockContextWithUserAndAchievement(bool achievementAlreadyAwarded = false)
        {
            var user = new User
            {
                Id = _testUserId,
                Username = "testuser",
                Balance = 100
            };

            var achievement = new Achievement
            {
                Id = _testAchievementId,
                Name = "Test Achievement",
                Type = AchievementType.Streak,
                Condition = "streak:7",
                RewardAmount = 50
            };

            var userAchievements = new List<UserAchievement>();
            if (achievementAlreadyAwarded)
            {
                userAchievements.Add(new UserAchievement
                {
                    UserId = _testUserId,
                    AchievementId = _testAchievementId,
                    AwardedAt = DateTime.UtcNow
                });
            }

            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
            var mockAchievements = MockDbSetHelper.CreateMockDbSet(new List<Achievement> { achievement });
            var mockUserAchievements = MockDbSetHelper.CreateMockDbSet(userAchievements);

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task Handle_AchievementAndUserExist_ReturnsTrue()
        {
            // Arrange
            SetupMockContextWithUserAndAchievement();
            var command = new AwardAchievementCommand
            {
                UserId = _testUserId,
                AchievementId = _testAchievementId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AchievementAlreadyAwarded_ReturnsFalse()
        {
            // Arrange
            SetupMockContextWithUserAndAchievement(achievementAlreadyAwarded: true);
            var command = new AwardAchievementCommand
            {
                UserId = _testUserId,
                AchievementId = _testAchievementId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var command = new AwardAchievementCommand
            {
                UserId = Guid.NewGuid(), // non-existent user
                AchievementId = _testAchievementId
            };

            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User>());
            var mockAchievements = MockDbSetHelper.CreateMockDbSet(new List<Achievement>
            {
                new Achievement { Id = _testAchievementId }
            });

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AchievementNotFound_ReturnsFalse()
        {
            // Arrange
            var command = new AwardAchievementCommand
            {
                UserId = _testUserId,
                AchievementId = Guid.NewGuid() // non-existent achievement
            };

            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User>
            {
                new User { Id = _testUserId }
            });
            var mockAchievements = MockDbSetHelper.CreateMockDbSet(new List<Achievement>());

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SuccessfulAward_UpdatesUserBalance()
        {
            // Arrange
            SetupMockContextWithUserAndAchievement();
            var command = new AwardAchievementCommand
            {
                UserId = _testUserId,
                AchievementId = _testAchievementId
            };

            var initialBalance = 100;

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockContext.Verify(c => c.Users, Times.Once);

            // Verify user was updated
            var userEntry = _mockContext.Object.Users.First();
            Assert.Equal(initialBalance + 50, userEntry.Balance); // 50 is the reward amount
        }
        // Добавьте к существующим тестам AwardAchievementCommandHandlerTests
        [Fact]
        public async Task Handle_AchievementWithReward_UpdatesUserBalance()
        {
            // Arrange
            SetupMockContextWithUserAndAchievement();
            var command = new AwardAchievementCommand
            {
                UserId = _testUserId,
                AchievementId = _testAchievementId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);

            // Verify user balance was updated
            var user = _mockContext.Object.Users.First();
            Assert.Equal(150, user.Balance); // Initial 100 + reward 50
        }

        [Fact]
        public async Task Handle_DatabaseError_ReturnsFalse()
        {
            // Arrange
            SetupMockContextWithUserAndAchievement();
            var command = new AwardAchievementCommand
            {
                UserId = _testUserId,
                AchievementId = _testAchievementId
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()));
        }
    }
}
