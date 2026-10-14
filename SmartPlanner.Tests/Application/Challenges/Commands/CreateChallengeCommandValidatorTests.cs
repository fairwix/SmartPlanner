// SmartPlanner.Tests/Application/Challenges/Validators/CreateChallengeCommandValidatorTests.cs
using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using SmartPlanner.Application.Challenges.Commands;
using SmartPlanner.Domain.Enums;
using Xunit;

namespace SmartPlanner.Tests.Application.Challenges.Validators
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
        [InlineData(" ")]
        public void Should_Have_Error_When_Title_Is_Empty(string title)
        {
            // Arrange
            var command = new CreateChallengeCommand { Title = title };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Should_Have_Error_When_Title_Is_Too_Long()
        {
            // Arrange
            var command = new CreateChallengeCommand { Title = new string('A', 101) }; // 101 символ

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Should_Have_Error_When_Type_Is_Invalid()
        {
            // Arrange
            var command = new CreateChallengeCommand { Type = "InvalidType" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Type);
        }

        [Fact]
        public void Should_Have_Error_When_Type_Is_Empty()
        {
            // Arrange
            var command = new CreateChallengeCommand { Type = "" };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Type);
        }

        [Fact]
        public void Should_Have_Error_When_StartDate_Is_In_The_Past()
        {
            // Arrange
            var command = new CreateChallengeCommand { StartDate = DateTime.UtcNow.AddDays(-1) };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate);
        }

        [Fact]
        public void Should_Have_Error_When_EndDate_Is_Before_StartDate()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(2);
            var endDate = DateTime.UtcNow.AddDays(1); // До startDate
            var command = new CreateChallengeCommand { StartDate = startDate, EndDate = endDate };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate);
        }

        [Fact]
        public void Should_Have_Error_When_TargetValue_Is_NonPositive()
        {
            // Arrange
            var command = new CreateChallengeCommand { TargetValue = 0 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetValue);
        }

         [Fact]
        public void Should_Have_Error_When_TargetValue_Is_Negative()
        {
            // Arrange
            var command = new CreateChallengeCommand { TargetValue = -10 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetValue);
        }

        [Fact]
        public void Should_Have_Error_When_CreatedBy_Is_Empty()
        {
            // Arrange
            var command = new CreateChallengeCommand { CreatedBy = Guid.Empty };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CreatedBy);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Command_Is_Valid()
        {
            // Arrange
            var command = new CreateChallengeCommand
            {
                Title = "Valid Challenge",
                Description = "A valid challenge",
                Type = "Exercise", // Допустимое значение
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(7),
                IsGroupChallenge = false,
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
