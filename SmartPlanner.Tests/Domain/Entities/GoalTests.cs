using Xunit;
using SmartPlanner.Domain.Entities;
using System;
using SmartPlanner.Domain.Exceptions;

namespace SmartPlanner.Tests.Domain.Entities
{
    public class GoalTests
    {
        [Fact]
        public void Constructor_ValidData_CreatesGoal()
        {
            // Arrange
            var title = "Test Goal";
            var userId = Guid.NewGuid();

            // Act
            var goal = new Goal
            {
                Title = title,
                Description = "Test Description",
                Category = GoalCategory.Sports,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                UserId = userId
            };

            // Assert
            Assert.Equal(title, goal.Title);
            Assert.Equal(userId, goal.UserId);
            Assert.False(goal.IsCompleted);
            Assert.Equal(0, goal.CurrentValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Title_SetEmptyValue_ThrowsArgumentException(string invalidTitle)
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Valid Title",
                UserId = Guid.NewGuid()
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => goal.Title = invalidTitle);
        }

        [Fact]
        public void UpdateProgress_ValidValue_UpdatesProgress()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                TargetValue = 100
            };

            // Act
            goal.UpdateProgress(50);

            // Assert
            Assert.Equal(50, goal.CurrentValue);
            Assert.Equal(50.0, goal.GetProgressPercentage());
            Assert.Single(goal.ProgressHistory);
        }

        [Fact]
        public void UpdateProgress_ExceedsTargetValue_CompletesGoal()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = userId,
                TargetValue = 100,
                RewardAmount = 50
            };

            // Act
            goal.UpdateProgress(100);

            // Assert
            Assert.True(goal.IsCompleted);
            Assert.Equal(100, goal.CurrentValue);
        }

        [Fact]
        public void UpdateProgress_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                TargetValue = 100
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => goal.UpdateProgress(-10));
        }

        [Fact]
        public void UpdateProgress_GreaterThanTarget_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                TargetValue = 100
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => goal.UpdateProgress(150));
        }

        [Fact]
        public void GetProgressPercentage_TargetZero_ReturnsZero()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                TargetValue = 0,
                CurrentValue = 50
            };

            // Act
            var percentage = goal.GetProgressPercentage();

            // Assert
            Assert.Equal(0, percentage);
        }

        [Fact]
        public void IsExpired_DueDatePassed_ReturnsTrue()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(-1)
            };

            // Act
            var isExpired = goal.IsExpired();

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void IsExpired_DueDateFuture_ReturnsFalse()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var isExpired = goal.IsExpired();

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public void IsValid_ValidGoal_ReturnsTrue()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Valid Goal",
                Description = "Description",
                Category = GoalCategory.Sports,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                CurrentValue = 0,
                UserId = Guid.NewGuid()
            };

            // Act
            var isValid = goal.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValid_EmptyUserId_ReturnsFalse()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.Empty
            };

            // Act
            var isValid = goal.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsOnTrack_ProgressOnSchedule_ReturnsTrue()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                TargetValue = 100,
                CurrentValue = 50,
                DueDate = DateTime.UtcNow.AddDays(5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            // Act
            var isOnTrack = goal.IsOnTrack();

            // Assert
            Assert.True(isOnTrack);
        }

        [Fact]
        public void CanBeEdited_NotCompletedNotExpired_ReturnsTrue()
        {
            // Arrange
            var goal = new Goal
            {
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(1),
                IsCompleted = false
            };

            // Act
            var canBeEdited = goal.CanBeEdited();

            // Assert
            Assert.True(canBeEdited);
        }
    }
}
