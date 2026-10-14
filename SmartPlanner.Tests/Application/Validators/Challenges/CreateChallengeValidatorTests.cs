// Tests/Unit/Application/Validators/Challenges/CreateChallengeValidatorTests.cs
using FluentAssertions;
using FluentValidation.TestHelper;
using SmartPlanner.Application.DTOs.Challenge;
using SmartPlanner.Application.Common.Validators.Challenges;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Validators.Challenges
{
    public class CreateChallengeValidatorTests
    {
        private readonly CreateChallengeValidator _validator;

        public CreateChallengeValidatorTests()
        {
            _validator = new CreateChallengeValidator();
        }

        [Fact]
        public void Should_HaveError_WhenTitleIsEmpty()
        {
            // Arrange
            var request = new CreateChallengeRequest(
                string.Empty, // Empty title
                "Description",
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Название челленджа обязательно");
        }

        [Fact]
        public void Should_HaveError_WhenTitleTooLong()
        {
            // Arrange
            var longTitle = new string('A', 101); // 101 characters
            var request = new CreateChallengeRequest(
                longTitle,
                "Description",
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Название челленджа не должно превышать 100 символов");
        }

        [Fact]
        public void Should_HaveError_WhenDescriptionTooLong()
        {
            // Arrange
            var longDescription = new string('A', 501); // 501 characters
            var request = new CreateChallengeRequest(
                "Valid Title",
                longDescription,
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Описание челленджа не должно превышать 500 символов");
        }

        [Fact]
        public void Should_NotHaveError_WhenDescriptionIsNull()
        {
            // Arrange
            var request = new CreateChallengeRequest(
                "Valid Title",
                null!, // Null description
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Should_HaveError_WhenStartDateInPast()
        {
            // Arrange
            var request = new CreateChallengeRequest(
                "Valid Title",
                "Description",
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(-1), // Past date
                DateTime.UtcNow.AddDays(30),
                false,
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                .WithErrorMessage("Дата начала должна быть в будущем");
        }

        [Fact]
        public void Should_HaveError_WhenEndDateBeforeStartDate()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(5);
            var request = new CreateChallengeRequest(
                "Valid Title",
                "Description",
                ChallengeType.StepCount,
                startDate,
                startDate.AddDays(-1), // End before start
                false,
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                .WithErrorMessage("Дата окончания должна быть после даты начала");
        }

        [Fact]
        public void Should_HaveError_WhenTargetValueZero()
        {
            // Arrange
            var request = new CreateChallengeRequest(
                "Valid Title",
                "Description",
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
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
            var request = new CreateChallengeRequest(
                "Valid Title",
                "Description",
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
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
        public void Should_BeValid_WhenAllFieldsCorrect()
        {
            // Arrange
            var request = new CreateChallengeRequest(
                "Valid Title",
                "Valid Description",
                ChallengeType.Exercise,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                true,
                1000,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(ChallengeType.StepCount)]
        [InlineData(ChallengeType.Reading)]
        [InlineData(ChallengeType.Exercise)]
        [InlineData(ChallengeType.Learning)]
        [InlineData(ChallengeType.Custom)]
        public void Should_AcceptAllChallengeTypes(ChallengeType type)
        {
            // Arrange
            var request = new CreateChallengeRequest(
                "Valid Title",
                "Description",
                type,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
                100,
                Guid.NewGuid()
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Type);
        }

        [Fact]
        public void Should_NotValidateCreatedBy()
        {
            // Arrange - CreatedBy is empty guid
            var request = new CreateChallengeRequest(
                "Valid Title",
                "Description",
                ChallengeType.StepCount,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(30),
                false,
                100,
                Guid.Empty
            );

            // Act
            var result = _validator.TestValidate(request);

            // Assert - No validation for CreatedBy
            result.ShouldNotHaveValidationErrorFor(x => x.CreatedBy);
        }
    }
}
