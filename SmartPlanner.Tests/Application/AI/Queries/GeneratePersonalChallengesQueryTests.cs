// SmartPlanner.Tests/Application/AI/Queries/GeneratePersonalChallengesQueryTests.cs
using System;
using SmartPlanner.Application.AI.Queries;
using Xunit;

namespace SmartPlanner.Tests.Application.AI.Queries
{
    public class GeneratePersonalChallengesQueryTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var count = 5;

            // Act
            var query = new GeneratePersonalChallengesQuery { UserId = userId, Count = count };

            // Assert
            Assert.Equal(userId, query.UserId);
            Assert.Equal(count, query.Count);
        }

        [Fact]
        public void Constructor_HasDefaultCount()
        {
            // Act
            var query = new GeneratePersonalChallengesQuery { UserId = Guid.NewGuid() }; // Не указываем Count

            // Assert
            Assert.Equal(3, query.Count); // Проверяем, что значение по умолчанию 3
        }
    }
}
