// Tests/Unit/Application/Goals/Dtos/GoalDtoTests.cs
using FluentAssertions;
using SmartPlanner.Application.Goals.Dtos;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Goals.Dtos
{
    public class CreateGoalDtoTests
    {
        [Fact]
        public void CreateGoalDto_ShouldCreateSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Исправлено: правильное создание record
            var dto = new CreateGoalDto(
                Title: "Learn French",
                Description: "Complete French course",
                Category: "Education",
                Priority: "High",
                DueDate: DateTime.UtcNow.AddMonths(3),
                TargetValue: 100,
                UserId: userId,
                IsAiGenerated: false,
                RewardAmount: 50
            );

            // Assert
            dto.Should().NotBeNull();
            dto.Title.Should().Be("Learn French");
            dto.Category.Should().Be("Education");
            dto.Priority.Should().Be("High");
            dto.TargetValue.Should().Be(100);
            dto.UserId.Should().Be(userId);
            dto.RewardAmount.Should().Be(50);
            dto.IsAiGenerated.Should().BeFalse();
        }

        [Fact]
        public void CreateGoalDto_ShouldHaveDefaultValues()
        {
            // Исправлено: используем значения по умолчанию
            var dto = new CreateGoalDto(
                Title: "Test Goal",
                Description: "Test Description",
                Category: "Health",
                Priority: "Medium",
                DueDate: DateTime.UtcNow.AddDays(30),
                TargetValue: 100,
                UserId: Guid.NewGuid()
                // IsAiGenerated и RewardAmount будут значениями по умолчанию
            );

            // Assert
            dto.IsAiGenerated.Should().BeFalse(); // Default
            dto.RewardAmount.Should().Be(10); // Default
        }
    }

    public class UpdateGoalDtoTests
    {
        [Fact]
        public void UpdateGoalDto_ShouldAllowPartialUpdates()
        {
            // Исправлено: правильное создание record
            var dto = new UpdateGoalDto(
                Title: "Updated Title",
                Description: "Updated Description",
                Category: "Health",
                Priority: null,
                DueDate: DateTime.UtcNow.AddDays(60),
                TargetValue: 200
            );

            // Assert
            dto.Should().NotBeNull();
            dto.Title.Should().Be("Updated Title");
            dto.Description.Should().Be("Updated Description");
            dto.Category.Should().Be("Health");
            dto.Priority.Should().BeNull();
            dto.DueDate.Should().NotBeNull();
            dto.TargetValue.Should().Be(200);
        }

        [Fact]
        public void UpdateGoalDto_ShouldAllowAllNullUpdates()
        {
            // Исправлено: все параметры null
            var dto = new UpdateGoalDto(
                Title: null,
                Description: null,
                Category: null,
                Priority: null,
                DueDate: null,
                TargetValue: null
            );

            // Assert
            dto.Title.Should().BeNull();
            dto.Description.Should().BeNull();
            dto.Category.Should().BeNull();
            dto.Priority.Should().BeNull();
            dto.DueDate.Should().BeNull();
            dto.TargetValue.Should().BeNull();
        }
    }

    public class GoalDtoTests
    {
        [Fact]
        public void GoalDto_ShouldCreateSuccessfully()
        {
            // Исправлено: правильное создание record
            var dto = new GoalDto(
                Id: Guid.NewGuid(),
                CreatedAt: DateTime.UtcNow.AddDays(-30),
                UpdatedAt: DateTime.UtcNow,
                Title: "Fitness Goal",
                Description: "Go to gym 3 times a week",
                Category: "Health",
                Priority: "Medium",
                DueDate: DateTime.UtcNow.AddMonths(1),
                TargetValue: 12,
                CurrentValue: 3,
                ProgressPercentage: 25.0,
                IsCompleted: false,
                IsAiGenerated: true,
                RewardAmount: 100,
                UserId: Guid.NewGuid(),
                IsExpired: false,
                IsOnTrack: true
            );

            // Assert
            dto.Should().NotBeNull();
            dto.Title.Should().Be("Fitness Goal");
            dto.Category.Should().Be("Health");
            dto.Priority.Should().Be("Medium");
            dto.TargetValue.Should().Be(12);
            dto.CurrentValue.Should().Be(3);
            dto.ProgressPercentage.Should().Be(25.0);
            dto.IsCompleted.Should().BeFalse();
            dto.IsAiGenerated.Should().BeTrue();
            dto.RewardAmount.Should().Be(100);
            dto.IsExpired.Should().BeFalse();
            dto.IsOnTrack.Should().BeTrue();
        }
    }
}
