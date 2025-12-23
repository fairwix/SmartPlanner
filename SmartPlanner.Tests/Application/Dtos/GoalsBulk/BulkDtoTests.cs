// Tests/Unit/Application/DTOs/GoalsBulk/BulkDtoTests.cs
using FluentAssertions;
using SmartPlanner.API.Dtos.GoalsBulk;
using Xunit;

namespace SmartPlanner.Application.UnitTests.DTOs.GoalsBulk
{
    public class BulkDeleteGoalsRequestTests
    {
        [Fact]
        public void BulkDeleteGoalsRequest_ShouldInitializeLists()
        {
            // Act
            var request = new BulkDeleteGoalsRequest();

            // Assert
            request.Should().NotBeNull();
            request.GoalIds.Should().NotBeNull();
            request.GoalIds.Should().BeEmpty();
        }

        [Fact]
        public void BulkDeleteGoalsRequest_ShouldAcceptMultipleIds()
        {
            // Arrange
            var ids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act
            var request = new BulkDeleteGoalsRequest
            {
                GoalIds = ids
            };

            // Assert
            request.GoalIds.Should().HaveCount(3);
            request.GoalIds.Should().BeEquivalentTo(ids);
        }

        [Fact]
        public void BulkDeleteGoalsRequest_ShouldAllowEmptyList()
        {
            // Act
            var request = new BulkDeleteGoalsRequest
            {
                GoalIds = new List<Guid>()
            };

            // Assert
            request.GoalIds.Should().BeEmpty();
        }
    }

    public class BulkCreateGoalsRequestTests
    {
        [Fact]
        public void BulkCreateGoalsRequest_ShouldInitializeLists()
        {
            // Act
            var request = new BulkCreateGoalsRequest();

            // Assert
            request.Should().NotBeNull();
            request.Goals.Should().NotBeNull();
            request.Goals.Should().BeEmpty();
        }

        [Fact]
        public void CreateGoalItemRequest_ShouldCreateSuccessfully()
        {
            // Arrange
            var title = "Goal Title";
            var description = "Goal Description";
            var deadline = DateTime.UtcNow.AddDays(30);
            var priority = 2;
            var category = "Work";
            var status = "Active";
            var tags = new List<string> { "urgent", "important" };
            var estimatedDuration = TimeSpan.FromHours(5);
            var parentGoalId = Guid.NewGuid();
            var recurrencePattern = "weekly";

            // Act
            var item = new CreateGoalItemRequest
            {
                Title = title,
                Description = description,
                Deadline = deadline,
                Priority = priority,
                Category = category,
                Status = status,
                Tags = tags,
                EstimatedDuration = estimatedDuration,
                ParentGoalId = parentGoalId,
                RecurrencePattern = recurrencePattern
            };

            // Assert
            item.Should().NotBeNull();
            item.Title.Should().Be(title);
            item.Description.Should().Be(description);
            item.Deadline.Should().Be(deadline);
            item.Priority.Should().Be(priority);
            item.Category.Should().Be(category);
            item.Status.Should().Be(status);
            item.Tags.Should().BeEquivalentTo(tags);
            item.EstimatedDuration.Should().Be(estimatedDuration);
            item.ParentGoalId.Should().Be(parentGoalId);
            item.RecurrencePattern.Should().Be(recurrencePattern);
        }

        [Fact]
        public void CreateGoalItemRequest_ShouldAllowNullableProperties()
        {
            // Act
            var item = new CreateGoalItemRequest
            {
                Title = "Simple Goal",
                Deadline = DateTime.UtcNow.AddDays(7),
                Priority = 1
            };

            // Assert
            item.Description.Should().BeNull();
            item.Category.Should().BeNull();
            item.Status.Should().BeNull();
            item.Tags.Should().BeNull();
            item.EstimatedDuration.Should().BeNull();
            item.ParentGoalId.Should().BeNull();
            item.RecurrencePattern.Should().BeNull();
        }
    }

    public class BulkUpdateGoalsRequestTests
    {
        [Fact]
        public void BulkUpdateGoalsRequest_ShouldInitializeLists()
        {
            // Act
            var request = new BulkUpdateGoalsRequest();

            // Assert
            request.Should().NotBeNull();
            request.Goals.Should().NotBeNull();
            request.Goals.Should().BeEmpty();
        }

        [Fact]
        public void UpdateGoalItemRequest_ShouldCreateSuccessfully()
        {
            // Arrange
            var id = Guid.NewGuid();
            var title = "Updated Title";
            var description = "Updated Description";
            var deadline = DateTime.UtcNow.AddDays(60);
            var priority = 3;

            // Act
            var item = new UpdateGoalItemRequest
            {
                Id = id,
                Title = title,
                Description = description,
                Deadline = deadline,
                Priority = priority
            };

            // Assert
            item.Should().NotBeNull();
            item.Id.Should().Be(id);
            item.Title.Should().Be(title);
            item.Description.Should().Be(description);
            item.Deadline.Should().Be(deadline);
            item.Priority.Should().Be(priority);
        }

        [Fact]
        public void UpdateGoalItemRequest_ShouldAllowNullDescription()
        {
            // Act
            var item = new UpdateGoalItemRequest
            {
                Id = Guid.NewGuid(),
                Title = "Title Only",
                Deadline = DateTime.UtcNow,
                Priority = 1,
                Description = null
            };

            // Assert
            item.Description.Should().BeNull();
        }
    }
}
