// SmartPlanner.Tests/Application/Achievements/Commands/CheckAndAwardAchievementsCommandTests.cs
using System;
using SmartPlanner.Application.Achievements.Commands;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Commands
{
    public class CheckAndAwardAchievementsCommandTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var command = new CheckAndAwardAchievementsCommand { UserId = userId };

            // Assert
            Assert.Equal(userId, command.UserId);
        }
    }
}
