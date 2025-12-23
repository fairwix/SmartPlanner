using Xunit;
using SmartPlanner.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SmartPlanner.Tests.Domain.Entities
{
    public class AchievementTests
    {
        [Fact]
        public void CanBeAwarded_StreakConditionMet_ReturnsTrue()
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
                Email = "test@example.com",
                StreakCount = 10
            };

            // Act
            var canBeAwarded = achievement.CanBeAwarded(user);

            // Assert
            Assert.True(canBeAwarded);
        }

        [Fact]
        public void CanBeAwarded_StreakConditionNotMet_ReturnsFalse()
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
                Email = "test@example.com",
                StreakCount = 5
            };

            // Act
            var canBeAwarded = achievement.CanBeAwarded(user);

            // Assert
            Assert.False(canBeAwarded);
        }

        [Fact]
        public void CanBeAwarded_GoalsCompletedConditionMet_ReturnsTrue()
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
                Username = "testuser",
                Email = "test@example.com",
                Goals = new List<Goal>()
            };

            // Добавляем завершенные цели пользователю
            for (int i = 0; i < 7; i++)
            {
                user.Goals.Add(new Goal
                {
                    Title = $"Goal {i}",
                    UserId = user.Id,
                    IsCompleted = true
                });
            }

            // Act
            var canBeAwarded = achievement.CanBeAwarded(user);

            // Assert
            Assert.True(canBeAwarded);
        }

        [Fact]
        public void CanBeAwarded_InvalidCondition_ReturnsFalse()
        {
            // Arrange
            var achievement = new Achievement
            {
                Name = "Test Achievement",
                Type = AchievementType.Streak,
                Condition = "invalid:format"
            };

            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                StreakCount = 10
            };

            // Act
            var canBeAwarded = achievement.CanBeAwarded(user);

            // Assert
            Assert.False(canBeAwarded);
        }
    }
}
