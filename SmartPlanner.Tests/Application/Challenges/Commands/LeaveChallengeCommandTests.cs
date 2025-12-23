// SmartPlanner.Tests/Application/Challenges/Commands/LeaveChallengeCommandTests.cs
using System;
using SmartPlanner.Application.Challenges.Commands;
using Xunit;

namespace SmartPlanner.Tests.Application.Challenges.Commands
{
    public class LeaveChallengeCommandTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var challengeId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            var command = new LeaveChallengeCommand { ChallengeId = challengeId, UserId = userId };

            // Assert
            Assert.Equal(challengeId, command.ChallengeId);
            Assert.Equal(userId, command.UserId);
        }
    }
}
