using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Data;
using SmartPlanner.Infrastructure.Services;
using Xunit;

namespace SmartPlanner.Tests.Infrastructure.Services
{
    public class AchievementCheckerServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AchievementCheckerService _service;

        public AchievementCheckerServiceTests()
        {
            // Configure in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _service = new AchievementCheckerService();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
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

            // Add test data to in-memory database
            _context.Users.Add(user);
            _context.Achievements.AddRange(achievements);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.CheckAndAwardEligibleAchievementsAsync(
                userId, _context, CancellationToken.None);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, a => a.Name == "Week Streak");
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
            Assert.True(meetsCondition);
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
            Assert.False(meetsCondition);
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
            Assert.True(meetsCondition);
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
            Assert.True(meetsCondition);
        }
    }
}
