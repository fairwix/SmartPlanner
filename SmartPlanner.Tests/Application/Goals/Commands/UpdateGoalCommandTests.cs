// SmartPlanner.Application.Tests/Goals/Commands/UpdateGoalCommandTests.cs
using SmartPlanner.Application.Goals.Commands;
using Xunit;

namespace SmartPlanner.Application.Tests.Goals.Commands
{
    public class UpdateGoalCommandTests
    {
        [Fact]
        public void UpdateGoalCommand_Properties_SetCorrectly()
        {
            // Arrange
            var goalId = Guid.NewGuid();
            var dueDate = DateTime.UtcNow.AddDays(30);

            // Act
            var command = new UpdateGoalCommand
            {
                GoalId = goalId,
                Title = "Updated Goal Title",
                Description = "Updated description",
                Category = "Personal",
                Priority = "High",
                DueDate = dueDate,
                TargetValue = 100
            };

            // Assert
            Assert.Equal(goalId, command.GoalId);
            Assert.Equal("Updated Goal Title", command.Title);
            Assert.Equal("Updated description", command.Description);
            Assert.Equal("Personal", command.Category);
            Assert.Equal("High", command.Priority);
            Assert.Equal(dueDate, command.DueDate);
            Assert.Equal(100, command.TargetValue);
        }

        [Fact]
        public void UpdateGoalCommand_NullableProperties_WorkCorrectly()
        {
            // Arrange
            var goalId = Guid.NewGuid();

            // Act
            var command = new UpdateGoalCommand
            {
                GoalId = goalId,
                Title = null,
                Description = null,
                Category = null,
                Priority = null,
                DueDate = null,
                TargetValue = null
            };

            // Assert
            Assert.Equal(goalId, command.GoalId);
            Assert.Null(command.Title);
            Assert.Null(command.Description);
            Assert.Null(command.Category);
            Assert.Null(command.Priority);
            Assert.Null(command.DueDate);
            Assert.Null(command.TargetValue);
        }

        [Fact]
        public void UpdateGoalCommand_AsRecord_WorksWithInitProperties()
        {
            // Arrange
            var goalId = Guid.NewGuid();

            // Act - Используем синтаксис record
            var command = new UpdateGoalCommand
            {
                GoalId = goalId,
                Title = "Test",
                Description = "Test Description"
            };

            // Assert
            Assert.Equal(goalId, command.GoalId);
            Assert.Equal("Test", command.Title);
            Assert.Equal("Test Description", command.Description);
        }

        [Fact]
        public void UpdateGoalCommand_WithPartialUpdate_WorksCorrectly()
        {
            // Arrange & Act
            var command = new UpdateGoalCommand
            {
                GoalId = Guid.NewGuid(),
                Title = "Only title updated",
                Description = null,
                Category = null,
                Priority = null,
                DueDate = null,
                TargetValue = null
            };

            // Assert
            Assert.Equal("Only title updated", command.Title);
            Assert.Null(command.Description);
            Assert.Null(command.Category);
            Assert.Null(command.Priority);
            Assert.Null(command.DueDate);
            Assert.Null(command.TargetValue);
        }

        [Fact]
        public void UpdateGoalCommand_ToString_ReturnsGoalId()
        {
            // Arrange
            var goalId = Guid.NewGuid();
            var command = new UpdateGoalCommand { GoalId = goalId };

            // Act
            var result = command.ToString();

            // Assert
            Assert.Contains(goalId.ToString(), result);
        }
    }
}
