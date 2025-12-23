// Tests/Unit/Application/Validators/Goal/CreateGoalValidatorTests.cs
using FluentAssertions;
using FluentValidation.TestHelper;
using SmartPlanner.Application.DTOs.Goal;
using SmartPlanner.Application.Common.Validators.Goal;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Validators.Goal
{
    public class CreateGoalValidatorTests
    {
        private readonly CreateGoalValidator _validator;

        public CreateGoalValidatorTests()
        {
            _validator = new CreateGoalValidator();
        }

        [Fact]
        public void Should_HaveError_WhenTitleIsEmpty()
        {
            // Arrange
            var request = new CreateGoalRequest(
                string.Empty, // Empty title
                "Description",
                GoalCategory.Health,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(30),
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Название цели не может быть пустым");
        }

        [Fact]
        public void Should_HaveError_WhenTitleTooLong()
        {
            // Arrange
            var longTitle = new string('A', 501); // 501 characters
            var request = new CreateGoalRequest(
                longTitle,
                "Description",
                GoalCategory.Health,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(30),
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Название цели слишком длинное");
        }

        [Fact]
        public void Should_NotHaveError_WhenDescriptionTooLong()
        {
            // Description validation is not specified in validator, so no error expected
            var longDescription = new string('A', 2001); // 2001 characters
            var request = new CreateGoalRequest(
                "Valid Title",
                longDescription,
                GoalCategory.Health,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(30),
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Should_HaveError_WhenDueDateInPast()
        {
            // Arrange
            var request = new CreateGoalRequest(
                "Valid Title",
                "Description",
                GoalCategory.Health,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(-1), // Past date
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DueDate)
                .WithErrorMessage("Дата завершения должна быть в будущем");
        }

        [Fact]
        public void Should_HaveError_WhenTargetValueZero()
        {
            // Arrange
            var request = new CreateGoalRequest(
                "Valid Title",
                "Description",
                GoalCategory.Health,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(30),
                0, // Zero target
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetValue)
                .WithErrorMessage("Целевое значение должно быть положительным");
        }

        [Fact]
        public void Should_HaveError_WhenTargetValueNegative()
        {
            // Arrange
            var request = new CreateGoalRequest(
                "Valid Title",
                "Description",
                GoalCategory.Health,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(30),
                -10, // Negative target
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetValue)
                .WithErrorMessage("Целевое значение должно быть положительным");
        }

        [Fact]
        public void Should_HaveError_WhenUserIdEmpty()
        {
            // Arrange
            var request = new CreateGoalRequest(
                "Valid Title",
                "Description",
                GoalCategory.Health,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(30),
                100,
                Guid.Empty // Empty guid
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID обязателен");
        }

        [Fact]
        public void Should_BeValid_WhenAllFieldsCorrect()
        {
            // Arrange
            var request = new CreateGoalRequest(
                "Valid Title",
                "Valid Description",
                GoalCategory.Education,
                GoalPriority.High,
                DateTime.UtcNow.AddDays(30),
                1000,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(GoalCategory.Sports)]
        [InlineData(GoalCategory.Education)]
        [InlineData(GoalCategory.Finance)]
        [InlineData(GoalCategory.Hobbies)]
        public void Should_AcceptAllGoalCategories(GoalCategory category)
        {
            // Arrange
            var request = new CreateGoalRequest(
                "Valid Title",
                "Description",
                category,
                GoalPriority.Medium,
                DateTime.UtcNow.AddDays(30),
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Category);
        }

        [Theory]
        [InlineData(GoalPriority.Low)]
        [InlineData(GoalPriority.Medium)]
        [InlineData(GoalPriority.High)]
        [InlineData(GoalPriority.Critical)]
        public void Should_AcceptAllGoalPriorities(GoalPriority priority)
        {
            // Arrange
            var request = new CreateGoalRequest(
                "Valid Title",
                "Description",
                GoalCategory.Health,
                priority,
                DateTime.UtcNow.AddDays(30),
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Priority);
        }
    }
}
