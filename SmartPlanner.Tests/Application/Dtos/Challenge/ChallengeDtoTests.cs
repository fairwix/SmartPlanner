// Tests/Unit/Application/DTOs/Goal/GoalDtoTests.cs
using FluentAssertions;
using SmartPlanner.Application.DTOs.Goal;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.DTOs.Goal
{
    public class CreateGoalRequestTests
    {
        [Fact]
        public void CreateGoalRequest_ShouldCreateSuccessfully()
        {
            // Arrange
            var title = "Learn Spanish";
            var description = "Complete Spanish course";
            var category = GoalCategory.Education;
            var priority = GoalPriority.High;
            var dueDate = DateTime.UtcNow.AddMonths(3);
            var targetValue = 100;
            var userId = Guid.NewGuid();

            // Act
            var request = new CreateGoalRequest(
                title,
                description,
                category,
                priority,
                dueDate,
                targetValue,
                userId
            );

            // Assert
            request.Should().NotBeNull();
            request.Title.Should().Be(title);
            request.Description.Should().Be(description);
            request.Category.Should().Be(category);
            request.Priority.Should().Be(priority);
            request.DueDate.Should().Be(dueDate);
            request.TargetValue.Should().Be(targetValue);
            request.UserId.Should().Be(userId);
        }

        [Theory]
        [InlineData(GoalCategory.Sports, GoalPriority.Low)]
        [InlineData(GoalCategory.Finance, GoalPriority.Critical)]
        [InlineData(GoalCategory.Hobbies, GoalPriority.Medium)]
        public void CreateGoalRequest_ShouldHandleAllCategoriesAndPriorities(GoalCategory category, GoalPriority priority)
        {
            // Act
            var request = new CreateGoalRequest(
                "Test Goal",
                "Test Description",
                category,
                priority,
                DateTime.UtcNow.AddDays(30),
                50,
                Guid.NewGuid()
            );

            // Assert
            request.Category.Should().Be(category);
            request.Priority.Should().Be(priority);
        }
    }

    public class GoalResponseTests
    {
        [Fact]
        public void GoalResponse_ShouldCreateSuccessfully()
        {
            // Arrange
            var id = Guid.NewGuid();
            var title = "Fitness Goal";
            var description = "Go to gym 3 times a week";
            var category = GoalCategory.Health;
            var priority = GoalPriority.Medium;
            var dueDate = DateTime.UtcNow.AddMonths(1);
            var targetValue = 12;
            var currentValue = 3;
            var progressPercentage = 25.0;
            var isCompleted = false;
            var isAiGenerated = true;
            var rewardAmount = 100;
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-7);
            var updatedAt = DateTime.UtcNow;
            var isExpired = false;
            var isOnTrack = true;

            // Act
            var response = new GoalResponse(
                id,
                title,
                description,
                category,
                priority,
                dueDate,
                targetValue,
                currentValue,
                progressPercentage,
                isCompleted,
                isAiGenerated,
                rewardAmount,
                userId,
                createdAt,
                updatedAt,
                isExpired,
                isOnTrack
            );

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().Be(id);
            response.Title.Should().Be(title);
            response.Category.Should().Be(category);
            response.Priority.Should().Be(priority);
            response.DueDate.Should().Be(dueDate);
            response.TargetValue.Should().Be(targetValue);
            response.CurrentValue.Should().Be(currentValue);
            response.ProgressPercentage.Should().Be(progressPercentage);
            response.IsCompleted.Should().Be(isCompleted);
            response.IsAiGenerated.Should().Be(isAiGenerated);
            response.RewardAmount.Should().Be(rewardAmount);
            response.UserId.Should().Be(userId);
            response.CreatedAt.Should().Be(createdAt);
            response.UpdatedAt.Should().Be(updatedAt);
            response.IsExpired.Should().Be(isExpired);
            response.IsOnTrack.Should().Be(isOnTrack);
        }

        [Fact]
        public void UpdateProgressRequest_ShouldCreateSuccessfully()
        {
            // Arrange
            var value = 50;

            // Act
            var request = new UpdateProgressRequest(value);

            // Assert
            request.Should().NotBeNull();
            request.Value.Should().Be(value);
        }

        [Fact]
        public void UpdateProgressRequest_ShouldAllowZero()
        {
            // Act
            var request = new UpdateProgressRequest(0);

            // Assert
            request.Value.Should().Be(0);
        }

        [Fact]
        public void UpdateProgressRequest_ShouldAllowNegative()
        {
            // Act
            var request = new UpdateProgressRequest(-10);

            // Assert
            request.Value.Should().Be(-10);
        }
    }

    public class UpdateGoalRequestTests
    {
        [Fact]
        public void UpdateGoalRequest_ShouldAllowPartialUpdates()
        {
            // Arrange
            var title = "Updated Title";
            var description = "Updated Description";
            var category = GoalCategory.Career;
            var priority = GoalPriority.High;
            var dueDate = DateTime.UtcNow.AddDays(60);
            var targetValue = 200;

            // Act - все поля заполнены
            var fullRequest = new UpdateGoalRequest(
                title,
                description,
                category,
                priority,
                dueDate,
                targetValue
            );

            // Act - частичное обновление
            var partialRequest = new UpdateGoalRequest(
                title,
                null,
                null,
                null,
                null,
                null
            );

            // Act - пустое обновление (все null)
            var emptyRequest = new UpdateGoalRequest(null, null, null, null, null, null);

            // Assert
            fullRequest.Should().NotBeNull();
            fullRequest.Title.Should().Be(title);
            fullRequest.Description.Should().Be(description);
            fullRequest.Category.Should().Be(category);
            fullRequest.Priority.Should().Be(priority);

            partialRequest.Should().NotBeNull();
            partialRequest.Title.Should().Be(title);
            partialRequest.Description.Should().BeNull();
            partialRequest.Category.Should().BeNull();

            emptyRequest.Should().NotBeNull();
            emptyRequest.Title.Should().BeNull();
            emptyRequest.Description.Should().BeNull();
        }

        [Fact]
        public void UpdateGoalRequest_ShouldHandleNullableProperties()
        {
            // Arrange
            var request = new UpdateGoalRequest(
                "New Title",
                null, // Description
                GoalCategory.Personal, // Category
                null, // Priority
                DateTime.UtcNow.AddDays(30), // DueDate
                null  // TargetValue
            );

            // Assert
            request.Title.Should().NotBeNull();
            request.Description.Should().BeNull();
            request.Category.Should().NotBeNull();
            request.Priority.Should().BeNull();
            request.DueDate.Should().NotBeNull();
            request.TargetValue.Should().BeNull();
        }
    }
}
