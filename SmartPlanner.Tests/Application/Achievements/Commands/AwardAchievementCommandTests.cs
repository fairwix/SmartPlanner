// SmartPlanner.Tests/Application/Achievements/Commands/AwardAchievementCommandTests.cs
using System;
using SmartPlanner.Application.Achievements.Commands;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Commands
{
    public class AwardAchievementCommandTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var achievementId = Guid.NewGuid();

            // Act
            var command = new AwardAchievementCommand { UserId = userId, AchievementId = achievementId };

            // Assert
            Assert.Equal(userId, command.UserId);
            Assert.Equal(achievementId, command.AchievementId);
        }
    }
}
