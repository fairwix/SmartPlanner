// SmartPlanner.Tests/Application/Achievements/Dtos/UserAchievementDtoTests.cs
using System;
using SmartPlanner.Application.Achievements.Dtos;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Dtos
{
    public class UserAchievementDtoTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow;
            var userId = Guid.NewGuid();
            var achievementId = Guid.NewGuid();
            var achievementName = "Test Achievement";
            var achievementDescription = "A test achievement";
            var badgeImage = "badge.png";
            var awardedAt = DateTime.UtcNow;

            // Act
            var dto = new UserAchievementDto(id, createdAt, updatedAt, userId, achievementId, achievementName, achievementDescription, badgeImage, awardedAt);

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(createdAt, dto.CreatedAt);
            Assert.Equal(updatedAt, dto.UpdatedAt);
            Assert.Equal(userId, dto.UserId);
            Assert.Equal(achievementId, dto.AchievementId);
            Assert.Equal(achievementName, dto.AchievementName);
            Assert.Equal(achievementDescription, dto.AchievementDescription);
            Assert.Equal(badgeImage, dto.BadgeImage);
            Assert.Equal(awardedAt, dto.AwardedAt);
        }
    }
}
