// SmartPlanner.Tests/Infrastructure/Services/AchievementCheckerServiceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using Xunit;
using FluentAssertions;
using SmartPlanner.Application.Services;

namespace SmartPlanner.Tests.Infrastructure.Services
{
    public class AchievementCheckerServiceTests
    {
        private readonly AchievementCheckerService _service;
        private readonly Mock<IApplicationDbContext> _mockContext;

        public AchievementCheckerServiceTests()
        {
            _service = new AchievementCheckerService();
            _mockContext = new Mock<IApplicationDbContext>();
        }

        [Fact]
        public async Task CheckAndAwardEligibleAchievementsAsync_UserWithStreak_ReturnsStreakAchievements()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                StreakCount = 10
            };

            var achievements = new List<Achievement>
            {
                new Achievement
                {
                    Id = Guid.NewGuid(),
                    Name = "Week Streak",
                    Type = AchievementType.Streak,
                    Condition = "streak:7",
                    RewardAmount = 100
                },
                new Achievement
                {
                    Id = Guid.NewGuid(),
                    Name = "Month Streak",
                    Type = AchievementType.Streak,
                    Condition = "streak:30",
                    RewardAmount = 500
                }
            };

            var userAchievements = new List<UserAchievement>();
            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
            var mockAchievements = MockDbSetHelper.CreateMockDbSet(achievements);
            var mockUserAchievements = MockDbSetHelper.CreateMockDbSet(userAchievements);

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);

            // Act
            var result = await _service.CheckAndAwardEligibleAchievementsAsync(
                userId, _mockContext.Object, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(a => a.Name == "Week Streak");
            result.Should().NotContain(a => a.Name == "Month Streak"); // Not eligible yet
        }

        [Fact]
        public async Task CheckAndAwardEligibleAchievementsAsync_UserWithCompletedGoals_ReturnsGoalAchievements()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser"
            };

            var goals = new List<Goal>
            {
                new Goal { Id = Guid.NewGuid(), UserId = userId, IsCompleted = true },
                new Goal { Id = Guid.NewGuid(), UserId = userId, IsCompleted = true },
                new Goal { Id = Guid.NewGuid(), UserId = userId, IsCompleted = false }
            };

            var achievements = new List<Achievement>
            {
                new Achievement
                {
                    Id = Guid.NewGuid(),
                    Name = "First Goal",
                    Type = AchievementType.GoalsCompleted,
                    Condition = "goals_completed:1",
                    RewardAmount = 50
                },
                new Achievement
                {
                    Id = Guid.NewGuid(),
                    Name = "Three Goals",
                    Type = AchievementType.GoalsCompleted,
                    Condition = "goals_completed:3",
                    RewardAmount = 150
                }
            };

            var userAchievements = new List<UserAchievement>();
            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
            var mockGoals = MockDbSetHelper.CreateMockDbSet(goals);
            var mockAchievements = MockDbSetHelper.CreateMockDbSet(achievements);
            var mockUserAchievements = MockDbSetHelper.CreateMockDbSet(userAchievements);

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);

            // Act
            var result = await _service.CheckAndAwardEligibleAchievementsAsync(
                userId, _mockContext.Object, CancellationToken.None);

            // Assert
            result.Should().Contain(a => a.Name == "First Goal");
            result.Should().NotContain(a => a.Name == "Three Goals"); // Only 2 completed
        }

        [Fact]
        public async Task CheckAndAwardEligibleAchievementsAsync_UserWithAchievements_ReturnsNoAchievements()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser"
            };

            var achievements = new List<Achievement>
            {
                new Achievement
                {
                    Id = Guid.NewGuid(),
                    Name = "Week Streak",
                    Type = AchievementType.Streak,
                    Condition = "streak:7",
                    RewardAmount = 100
                }
            };

            var userAchievements = new List<UserAchievement>
            {
                new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievements[0].Id,
                    AwardedAt = DateTime.UtcNow
                }
            };

            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
            var mockAchievements = MockDbSetHelper.CreateMockDbSet(achievements);
            var mockUserAchievements = MockDbSetHelper.CreateMockDbSet(userAchievements);

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);

            // Act
            var result = await _service.CheckAndAwardEligibleAchievementsAsync(
                userId, _mockContext.Object, CancellationToken.None);

            // Assert
            result.Should().BeEmpty(); // Already has this achievement
        }

        [Fact]
        public void MeetsAchievementCondition_StreakAchievementMet_ReturnsTrue()
        {
            // Arrange
            var achievement = new Achievement
            {
                Name = "Week Streak",
                Type = AchievementType.Streak,
                Condition = "streak:7"
            };
            var user = new User
            {
                Username = "testuser",
                StreakCount = 10
            };

            // Act
            var meetsCondition = _service.MeetsAchievementCondition(
                achievement, user, 0, 0);

            // Assert
            meetsCondition.Should().BeTrue();
        }

        [Fact]
        public void MeetsAchievementCondition_StreakAchievementNotMet_ReturnsFalse()
        {
            // Arrange
            var achievement = new Achievement
            {
                Name = "Week Streak",
                Type = AchievementType.Streak,
                Condition = "streak:7"
            };
            var user = new User
            {
                Username = "testuser",
                StreakCount = 5
            };

            // Act
            var meetsCondition = _service.MeetsAchievementCondition(
                achievement, user, 0, 0);

            // Assert
            meetsCondition.Should().BeFalse();
        }

        [Fact]
        public void MeetsAchievementCondition_GoalsCompletedAchievementMet_ReturnsTrue()
        {
            // Arrange
            var achievement = new Achievement
            {
                Name = "Goal Master",
                Type = AchievementType.GoalsCompleted,
                Condition = "goals_completed:5"
            };
            var user = new User
            {
                Username = "testuser"
            };

            // Act
            var meetsCondition = _service.MeetsAchievementCondition(
                achievement, user, 7, 0);

            // Assert
            meetsCondition.Should().BeTrue();
        }

        [Fact]
        public void MeetsAchievementCondition_FriendsAchievementMet_ReturnsTrue()
        {
            // Arrange
            var achievement = new Achievement
            {
                Name = "Social Butterfly",
                Type = AchievementType.Friends,
                Condition = "friends:3"
            };
            var user = new User
            {
                Username = "testuser"
            };

            // Act
            var meetsCondition = _service.MeetsAchievementCondition(
                achievement, user, 0, 5);

            // Assert
            meetsCondition.Should().BeTrue();
        }

        [Fact]
        public void MeetsAchievementCondition_InvalidConditionFormat_ReturnsFalse()
        {
            // Arrange
            var achievement = new Achievement
            {
                Name = "Test Achievement",
                Type = AchievementType.Streak,
                Condition = "invalid_format"
            };
            var user = new User
            {
                Username = "testuser",
                StreakCount = 10
            };

            // Act
            var meetsCondition = _service.MeetsAchievementCondition(
                achievement, user, 0, 0);

            // Assert
            meetsCondition.Should().BeFalse();
        }
    }
}
