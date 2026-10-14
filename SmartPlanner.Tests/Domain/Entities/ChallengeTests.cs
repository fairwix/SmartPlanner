using Xunit;
using SmartPlanner.Domain.Entities;
using System;
using System.Linq;

namespace SmartPlanner.Tests.Domain.Entities
{
    public class ChallengeTests
    {
        [Fact]
        public void IsActive_CurrentDateInRange_ReturnsTrue()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var isActive = challenge.IsActive();

            // Assert
            Assert.True(isActive);
        }

        [Fact]
        public void IsActive_CurrentDateBeforeStart_ReturnsFalse()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(7),
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var isActive = challenge.IsActive();

            // Assert
            Assert.False(isActive);
        }

        [Fact]
        public void IsActive_CurrentDateAfterEnd_ReturnsFalse()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow.AddDays(-1),
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var isActive = challenge.IsActive();

            // Assert
            Assert.False(isActive);
        }

        [Fact]
        public void GetGroupProgressPercentage_TargetPositive_ReturnsPercentage()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                TargetValue = 100,
                CurrentValue = 75,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var percentage = challenge.GetGroupProgressPercentage();

            // Assert
            Assert.Equal(75.0, percentage);
        }

        [Fact]
        public void GetGroupProgressPercentage_TargetZero_ReturnsZero()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                TargetValue = 0,
                CurrentValue = 100,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var percentage = challenge.GetGroupProgressPercentage();

            // Assert
            Assert.Equal(0, percentage);
        }

        [Fact]
        public void UpdateProgress_PositiveValue_IncreasesCurrentValue()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                TargetValue = 100,
                CurrentValue = 50,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            challenge.UpdateProgress(25);

            // Assert
            Assert.Equal(75, challenge.CurrentValue);
        }

        [Fact]
        public void UpdateProgress_ExceedsTarget_LimitsToTarget()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                TargetValue = 100,
                CurrentValue = 90,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            challenge.UpdateProgress(20);

            // Assert
            Assert.Equal(100, challenge.CurrentValue);
        }

        [Fact]
        public void UpdateProgress_NegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var challenge = new Challenge
            {
                Title = "Test Challenge",
                TargetValue = 100,
                CurrentValue = 50,
                CreatedBy = Guid.NewGuid()
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => challenge.UpdateProgress(-10));
        }

        [Fact]
        public void CanUserJoin_ActiveChallengeNotParticipating_ReturnsTrue()
        {
            // Arrange
            var challenge = new Challenge
            {
                Id = Guid.NewGuid(),
                Title = "Test Challenge",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                CreatedBy = Guid.NewGuid()
            };

            var userId = Guid.NewGuid();

            // Act
            var canJoin = challenge.CanUserJoin(userId);

            // Assert
            Assert.True(canJoin);
        }

        [Fact]
        public void AddParticipant_ValidUser_AddsParticipant()
        {
            // Arrange
            var challenge = new Challenge
            {
                Id = Guid.NewGuid(),
                Title = "Test Challenge",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                CreatedBy = Guid.NewGuid()
            };

            var userId = Guid.NewGuid();

            // Act
            challenge.AddParticipant(userId);

            // Assert
            Assert.Single(challenge.Participants);
            Assert.Equal(userId, challenge.Participants.First().UserId);
        }
    }
}
