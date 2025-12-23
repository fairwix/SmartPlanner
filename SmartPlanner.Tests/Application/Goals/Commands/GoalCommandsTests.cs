// // Tests/Unit/Application/Goals/Commands/GoalCommandsTests.cs
// using FluentAssertions;
// using SmartPlanner.Application.Common.Dtos;
// using SmartPlanner.Application.Goals.Commands;
// using SmartPlanner.Application.Goals.Dtos;
// using SmartPlanner.Domain.Entities;
// using Xunit;
//
// namespace SmartPlanner.Application.UnitTests.Goals.Commands
// {
//     public class GoalCommandsTests
//     {
//         public class CreateGoalCommandTests
//         {
//             [Fact]
//             public void CreateGoalCommand_ShouldCreateSuccessfully()
//             {
//                 // Arrange
//                 var userId = Guid.NewGuid();
//
//                 // Act
//                 var command = new CreateGoalCommand
//                 {
//                     Title = "Learn French",
//                     Description = "Complete French course",
//                     Category = GoalCategory.Education.ToString(),
//                     Priority = GoalPriority.High.ToString(),
//                     DueDate = DateTime.UtcNow.AddMonths(3),
//                     TargetValue = 100,
//                     UserId = userId
//                 };
//
//                 // Assert
//                 command.Should().NotBeNull();
//                 command.Title.Should().Be("Learn French");
//                 command.Category.Should().Be("Education");
//                 command.Priority.Should().Be("High");
//                 command.DueDate.Should().BeAfter(DateTime.UtcNow);
//                 command.TargetValue.Should().Be(100);
//                 command.UserId.Should().Be(userId);
//             }
//
//             [Fact]
//             public void CreateGoalCommand_ShouldHaveDefaultTargetValue()
//             {
//                 // Act
//                 var command = new CreateGoalCommand
//                 {
//                     Title = "Test Goal",
//                     Category = GoalCategory.Personal.ToString(),
//                     Priority = GoalPriority.Medium.ToString(),
//                     DueDate = DateTime.UtcNow.AddDays(30),
//                     UserId = Guid.NewGuid()
//                 };
//
//                 // Assert
//                 command.TargetValue.Should().Be(1); // Default from record
//             }
//         }
//
//         public class UpdateGoalCommandTests
//         {
//             [Fact]
//             public void UpdateGoalCommand_ShouldAllowPartialUpdates()
//             {
//                 // Arrange
//                 var goalId = Guid.NewGuid();
//
//                 // Act
//                 var command = new UpdateGoalCommand
//                 {
//                     GoalId = goalId,
//                     Title = "Updated Title",
//                     Description = null,
//                     Category = GoalCategory.Health.ToString(),
//                     Priority = null,
//                     DueDate = DateTime.UtcNow.AddDays(60),
//                     TargetValue = 200
//                 };
//
//                 // Assert
//                 command.GoalId.Should().Be(goalId);
//                 command.Title.Should().Be("Updated Title");
//                 command.Description.Should().BeNull();
//                 command.Category.Should().Be("Health");
//                 command.Priority.Should().BeNull();
//                 command.DueDate.Should().NotBeNull();
//                 command.TargetValue.Should().Be(200);
//             }
//
//             [Fact]
//             public void UpdateGoalCommand_ShouldAllowAllNullUpdates()
//             {
//                 // Act
//                 var command = new UpdateGoalCommand
//                 {
//                     GoalId = Guid.NewGuid(),
//                     Title = null,
//                     Description = null,
//                     Category = null,
//                     Priority = null,
//                     DueDate = null,
//                     TargetValue = null
//                 };
//
//                 // Assert
//                 command.Title.Should().BeNull();
//                 command.Description.Should().BeNull();
//                 command.Category.Should().BeNull();
//                 command.Priority.Should().BeNull();
//                 command.DueDate.Should().BeNull();
//                 command.TargetValue.Should().BeNull();
//             }
//         }
//
//         public class UpdateGoalProgressCommandTests
//         {
//             [Fact]
//             public void UpdateGoalProgressCommand_ShouldCreateSuccessfully()
//             {
//                 // Arrange
//                 var goalId = Guid.NewGuid();
//                 var progressValue = 50;
//
//                 // Act
//                 var command = new UpdateGoalProgressCommand
//                 {
//                     GoalId = goalId,
//                     Value = progressValue
//                 };
//
//                 // Assert
//                 command.GoalId.Should().Be(goalId);
//                 command.Value.Should().Be(progressValue);
//             }
//
//             [Theory]
//             [InlineData(0)]
//             [InlineData(-10)]
//             [InlineData(100)]
//             [InlineData(1000)]
//             public void UpdateGoalProgressCommand_ShouldAllowVariousValues(int value)
//             {
//                 // Act
//                 var command = new UpdateGoalProgressCommand
//                 {
//                     GoalId = Guid.NewGuid(),
//                     Value = value
//                 };
//
//                 // Assert
//                 command.Value.Should().Be(value);
//             }
//         }
//
//         public class DeleteGoalCommandTests
//         {
//             [Fact]
//             public void DeleteGoalCommand_ShouldCreateSuccessfully()
//             {
//                 // Arrange
//                 var goalId = Guid.NewGuid();
//
//                 // Act
//                 var command = new DeleteGoalCommand
//                 {
//                     GoalId = goalId
//                 };
//
//                 // Assert
//                 command.GoalId.Should().Be(goalId);
//             }
//         }
//
//         public class BulkCreateGoalsCommandTests
//         {
//             [Fact]
//             public void BulkCreateGoalsCommand_ShouldInitializeLists()
//             {
//                 // Act
//                 var command = new BulkCreateGoalsCommand();
//
//                 // Assert
//                 command.Should().NotBeNull();
//                 command.Goals.Should().NotBeNull();
//                 command.Goals.Should().BeEmpty();
//                 command.UserId.Should().Be(Guid.Empty);
//             }
//
//             [Fact]
//             public void BulkCreateGoalsCommand_ShouldCreateWithData()
//             {
//                 // Arrange
//                 var userId = Guid.NewGuid();
//                 var goals = new List<CreateGoalDto>
//                 {
//                     new CreateGoalDto
//                     {
//                         Title = "Goal 1",
//                         Category = "Health",
//                         DueDate = DateTime.UtcNow.AddDays(30),
//                         TargetValue = 100
//                     },
//                     new CreateGoalDto
//                     {
//                         Title = "Goal 2",
//                         Category = "Education",
//                         DueDate = DateTime.UtcNow.AddDays(60),
//                         TargetValue = 50
//                     }
//                 };
//
//                 // Act
//                 var command = new BulkCreateGoalsCommand
//                 {
//                     UserId = userId,
//                     Goals = goals
//                 };
//
//                 // Assert
//                 command.UserId.Should().Be(userId);
//                 command.Goals.Should().HaveCount(2);
//                 command.Goals.Should().BeEquivalentTo(goals);
//             }
//         }
//
//         public class BulkUpdateGoalsCommandTests
//         {
//             [Fact]
//             public void BulkUpdateGoalsCommand_ShouldInitializeLists()
//             {
//                 // Act
//                 var command = new BulkUpdateGoalsCommand();
//
//                 // Assert
//                 command.Should().NotBeNull();
//                 command.Goals.Should().NotBeNull();
//                 command.Goals.Should().BeEmpty();
//             }
//
//             [Fact]
//             public void BulkUpdateGoalItem_ShouldCreateSuccessfully()
//             {
//                 // Arrange
//                 var goalId = Guid.NewGuid();
//                 var updateData = new UpdateGoalDto
//                 {
//                     Title = "Updated Title",
//                 };
//
//                 // Act
//                 var item = new BulkUpdateGoalItem
//                 {
//                     GoalId = goalId,
//                     UpdateData = updateData
//                 };
//
//                 // Assert
//                 item.GoalId.Should().Be(goalId);
//                 item.UpdateData.Should().Be(updateData);
//             }
//         }
//
//         public class BulkDeleteGoalsCommandTests
//         {
//             [Fact]
//             public void BulkDeleteGoalsCommand_ShouldInitializeLists()
//             {
//                 // Act
//                 var command = new BulkDeleteGoalsCommand();
//
//                 // Assert
//                 command.Should().NotBeNull();
//                 command.GoalIds.Should().NotBeNull();
//                 command.GoalIds.Should().BeEmpty();
//             }
//
//             [Fact]
//             public void BulkDeleteGoalsCommand_ShouldCreateWithIds()
//             {
//                 // Arrange
//                 var goalIds = new List<Guid>
//                 {
//                     Guid.NewGuid(),
//                     Guid.NewGuid(),
//                     Guid.NewGuid()
//                 };
//
//                 // Act
//                 var command = new BulkDeleteGoalsCommand
//                 {
//                     GoalIds = goalIds
//                 };
//
//                 // Assert
//                 command.GoalIds.Should().HaveCount(3);
//                 command.GoalIds.Should().BeEquivalentTo(goalIds);
//             }
//         }
//
//         // Дополнительный тест в GoalCommandsTests.cs
//         public class BulkUpdateGoalItemTests
//         {
//             [Fact]
//             public void BulkUpdateGoalItem_ShouldCreateSuccessfully()
//             {
//                 // Arrange
//                 var goalId = Guid.NewGuid();
//
//                 // Исправлено: правильное создание UpdateGoalDto
//                 var updateData = new UpdateGoalDto(
//                     Title: "Updated Title",
//                     Description: "Updated Description",
//                     Category: "Health",
//                     Priority: "High",
//                     DueDate: DateTime.UtcNow.AddDays(60),
//                     TargetValue: 200
//                 );
//
//                 // Act
//                 var item = new BulkUpdateGoalItem
//                 {
//                     GoalId = goalId,
//                     UpdateData = updateData
//                 };
//
//                 // Assert
//                 item.Should().NotBeNull();
//                 item.GoalId.Should().Be(goalId);
//                 item.UpdateData.Should().Be(updateData);
//             }
//
//             [Fact]
//             public void BulkUpdateGoalItem_ShouldAllowNullUpdateData()
//             {
//                 // Act
//                 var item = new BulkUpdateGoalItem
//                 {
//                     GoalId = Guid.NewGuid(),
//                     UpdateData = null! // Может быть null в реальном коде
//                 };
//
//                 // Assert
//                 item.UpdateData.Should().BeNull();
//             }
//         }
//
//         public class GoalDtoTests
//         {
//             [Fact]
//             public void CreateGoalDto_ShouldCreateSuccessfully()
//             {
//                 // Arrange
//                 var dto = new CreateGoalDto
//                 {
//                     Title = "Test Goal",
//                     Description = "Test Description",
//                     Category = "Health",
//                     Priority = "High",
//                     DueDate = DateTime.UtcNow.AddDays(30),
//                     TargetValue = 100,
//                     RewardAmount = 50
//                 };
//
//                 // Assert
//                 dto.Should().NotBeNull();
//                 dto.Title.Should().Be("Test Goal");
//                 dto.Category.Should().Be("Health");
//                 dto.Priority.Should().Be("High");
//                 dto.TargetValue.Should().Be(100);
//                 dto.RewardAmount.Should().Be(50);
//             }
//
//             [Fact]
//             public void UpdateGoalDto_ShouldAllowNullValues()
//             {
//                 // Arrange
//                 var dto = new UpdateGoalDto
//                 {
//                     Title = null,
//                     Description = "Updated Description",
//                 };
//
//                 // Assert
//                 dto.Title.Should().BeNull();
//                 dto.Description.Should().Be("Updated Description");
//             }
//         }
//     }
// }
