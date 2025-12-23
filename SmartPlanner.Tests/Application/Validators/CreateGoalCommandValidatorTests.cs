// SmartPlanner.Tests/Application/Goals/Validators/CreateGoalCommandValidatorTests.cs
using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using SmartPlanner.Application.Goals.Commands;
using Xunit;

namespace SmartPlanner.Tests.Application.Goals.Validators
{
    public class CreateGoalCommandValidatorTests
    {
        private readonly CreateGoalCommandValidator _validator;

        public CreateGoalCommandValidatorTests()
        {
            _validator = new CreateGoalCommandValidator(); // Реализация должна быть в основном проекте
        }

        [Fact]
        public void Should_Have_Error_When_Title_Is_Empty()
        {
            // Arrange
            var command = new CreateGoalCommand { Title = "", Category = "Sports", Priority = "High", DueDate = DateTime.UtcNow.AddDays(7), TargetValue = 100, UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Should_Have_Error_When_Category_Is_Invalid()
        {
            // Arrange
            var command = new CreateGoalCommand { Title = "Test Goal", Category = "InvalidCategory", Priority = "High", DueDate = DateTime.UtcNow.AddDays(7), TargetValue = 100, UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Category);
        }

        [Fact]
        public void Should_Have_Error_When_Priority_Is_Invalid()
        {
            // Arrange
            var command = new CreateGoalCommand { Title = "Test Goal", Category = "Sports", Priority = "InvalidPriority", DueDate = DateTime.UtcNow.AddDays(7), TargetValue = 100, UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Priority);
        }

        [Fact]
        public void Should_Have_Error_When_DueDate_Is_In_The_Past()
        {
            // Arrange
            var command = new CreateGoalCommand { Title = "Test Goal", Category = "Sports", Priority = "High", DueDate = DateTime.UtcNow.AddDays(-1), TargetValue = 100, UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DueDate);
        }

        [Fact]
        public void Should_Have_Error_When_TargetValue_Is_Negative()
        {
            // Arrange
            var command = new CreateGoalCommand { Title = "Test Goal", Category = "Sports", Priority = "High", DueDate = DateTime.UtcNow.AddDays(7), TargetValue = -10, UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetValue);
        }

        [Fact]
        public void Should_Have_Error_When_UserId_Is_Empty()
        {
            // Arrange
            var command = new CreateGoalCommand { Title = "Test Goal", Category = "Sports", Priority = "High", DueDate = DateTime.UtcNow.AddDays(7), TargetValue = 100, UserId = Guid.Empty };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Command_Is_Valid()
        {
            // Arrange
            var command = new CreateGoalCommand { Title = "Valid Goal", Category = "Sports", Priority = "High", DueDate = DateTime.UtcNow.AddDays(7), TargetValue = 100, UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
