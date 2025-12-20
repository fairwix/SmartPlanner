// SmartPlanner.Tests/Application/Validators/CreateChallengeCommandValidatorTests.cs
using Xunit;
using FluentValidation.TestHelper;
using SmartPlanner.Application.Challenges.Commands;
using System;

namespace SmartPlanner.Tests.Application.Validators
{
    public class CreateChallengeCommandValidatorTests
    {
        private readonly CreateChallengeCommandValidator _validator;

        public CreateChallengeCommandValidatorTests()
        {
            _validator = new CreateChallengeCommandValidator();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_EmptyTitle_ShouldHaveValidationError(string title)
        {
            // Arrange
            var command = new CreateChallengeCommand
            {
                Title = title,
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(8),
                TargetValue = 100,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateChallengeCommand
            {
                Title = new string('a', 101),
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(8),
                TargetValue = 100,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title cannot exceed 100 characters");
        }

        [Fact]
        public void Validate_PastStartDate_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateChallengeCommand
            {
                Title = "Test Challenge",
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                .WithErrorMessage("Start date must be in the future");
        }

        [Fact]
        public void Validate_EndDateBeforeStartDate_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateChallengeCommand
            {
                Title = "Test Challenge",
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(1),
                TargetValue = 100,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                .WithErrorMessage("End date must be after start date");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Validate_NonPositiveTargetValue_ShouldHaveValidationError(int targetValue)
        {
            // Arrange
            var command = new CreateChallengeCommand
            {
                Title = "Test Challenge",
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(8),
                TargetValue = targetValue,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetValue)
                .WithErrorMessage("Target value must be positive");
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPassValidation()
        {
            // Arrange
            var command = new CreateChallengeCommand
            {
                Title = "Valid Challenge",
                Description = "Valid Description",
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(8),
                IsGroupChallenge = true,
                TargetValue = 100,
                CreatedBy = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
