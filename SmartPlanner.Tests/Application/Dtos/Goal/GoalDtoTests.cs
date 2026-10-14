// SmartPlanner.Tests/Application/Dtos/GoalDtoTests.cs
using Xunit;
using SmartPlanner.Application.Goals.Dtos;
using System;

namespace SmartPlanner.Tests.Application.Dtos
{
    public class GoalDtoTests
    {
        [Fact]
        public void GoalDto_Creation_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow;

            var goalDto = new GoalDto(
                id,
                createdAt,
                updatedAt,
                "Test Goal",
                "Test Description",
                "Sports",
                "Medium",
                DateTime.UtcNow.AddDays(7),
                100,
                50,
                50.0,
                false,
                false,
                10,
                Guid.NewGuid(),
                false,
                true
            );

            // Assert
            Assert.Equal(id, goalDto.Id);
            Assert.Equal("Test Goal", goalDto.Title);
            Assert.Equal("Test Description", goalDto.Description);
            Assert.Equal("Sports", goalDto.Category);
            Assert.Equal("Medium", goalDto.Priority);
            Assert.Equal(100, goalDto.TargetValue);
            Assert.Equal(50, goalDto.CurrentValue);
            Assert.Equal(50.0, goalDto.ProgressPercentage);
            Assert.False(goalDto.IsCompleted);
            Assert.False(goalDto.IsExpired);
            Assert.True(goalDto.IsOnTrack);
        }

        [Fact]
        public void GoalDto_CompletedGoal_HasCorrectProgress()
        {
            // Arrange & Act
            var goalDto = new GoalDto(
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow,
                "Completed Goal",
                "Description",
                "Sports",
                "Medium",
                DateTime.UtcNow.AddDays(7),
                100,
                100,
                100.0,
                true,
                false,
                10,
                Guid.NewGuid(),
                false,
                true
            );

            // Assert
            Assert.True(goalDto.IsCompleted);
            Assert.Equal(100.0, goalDto.ProgressPercentage);
        }
    }
}
