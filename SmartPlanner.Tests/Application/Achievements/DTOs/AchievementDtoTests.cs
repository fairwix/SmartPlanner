// SmartPlanner.Tests/Application/Achievements/Dtos/AchievementDtoTests.cs
using System;
using SmartPlanner.Application.Achievements.Dtos;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Dtos
{
    public class AchievementDtoTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow;
            var name = "Test Achievement";
            var description = "A test achievement";
            var badgeImage = "badge.png";
            var rewardAmount = 100;
            var type = "Streak";
            var condition = "streak:7";

            // Act
            var dto = new AchievementDto(id, createdAt, updatedAt, name, description, badgeImage, rewardAmount, type, condition);

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(createdAt, dto.CreatedAt);
            Assert.Equal(updatedAt, dto.UpdatedAt);
            Assert.Equal(name, dto.Name);
            Assert.Equal(description, dto.Description);
            Assert.Equal(badgeImage, dto.BadgeImage);
            Assert.Equal(rewardAmount, dto.RewardAmount);
            Assert.Equal(type, dto.Type);
            Assert.Equal(condition, dto.Condition);
        }
    }
}
