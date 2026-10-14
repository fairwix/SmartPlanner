// SmartPlanner.Tests/Application/Achievements/Queries/GetUserAchievementsQueryTests.cs
using System;
using SmartPlanner.Application.Achievements.Queries;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Queries
{
    public class GetUserAchievementsQueryTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var query = new GetUserAchievementsQuery { UserId = userId };

            // Assert
            Assert.Equal(userId, query.UserId);
        }
    }
}
