// SmartPlanner.Tests/Application/Achievements/Queries/GetAchievementsQueryTests.cs
using SmartPlanner.Application.Achievements.Queries;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Queries
{
    public class GetAchievementsQueryTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var queryWithoutType = new GetAchievementsQuery(); // AchievementType по умолчанию null
            var queryWithType = new GetAchievementsQuery { AchievementType = "Streak" };

            // Assert
            Assert.Null(queryWithoutType.AchievementType);
            Assert.Equal("Streak", queryWithType.AchievementType);
        }
    }
}
