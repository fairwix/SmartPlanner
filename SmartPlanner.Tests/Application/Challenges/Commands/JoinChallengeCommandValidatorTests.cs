// SmartPlanner.Tests/Application/Challenges/Validators/JoinChallengeCommandValidatorTests.cs
using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using SmartPlanner.Application.Challenges.Commands;
using Xunit;

namespace SmartPlanner.Tests.Application.Challenges.Validators
{
    public class JoinChallengeCommandValidatorTests
    {
        private readonly JoinChallengeCommandValidator _validator;

        public JoinChallengeCommandValidatorTests()
        {
            _validator = new JoinChallengeCommandValidator();
        }

        [Fact]
        public void Should_Have_Error_When_ChallengeId_Is_Empty()
        {
            // Arrange
            var command = new JoinChallengeCommand { ChallengeId = Guid.Empty, UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ChallengeId);
        }

        [Fact]
        public void Should_Have_Error_When_UserId_Is_Empty()
        {
            // Arrange
            var command = new JoinChallengeCommand { ChallengeId = Guid.NewGuid(), UserId = Guid.Empty };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Command_Is_Valid()
        {
            // Arrange
            var command = new JoinChallengeCommand { ChallengeId = Guid.NewGuid(), UserId = Guid.NewGuid() };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
