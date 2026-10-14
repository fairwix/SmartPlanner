// SmartPlanner.Tests/Application/Achievements/Queries/GetAchievementByIdQueryTests.cs
using System;
using SmartPlanner.Application.Achievements.Queries;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Queries
{
    public class GetAchievementByIdQueryTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var achievementId = Guid.NewGuid();

            // Act
            var query = new GetAchievementByIdQuery { AchievementId = achievementId };

            // Assert
            Assert.Equal(achievementId, query.AchievementId);
        }
    }
}
