// SmartPlanner.Tests/Application/Common/Behaviors/ValidationBehaviorTests.cs
using Xunit;
using FluentValidation;
using MediatR;
using Moq;
using SmartPlanner.Application.Common.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace SmartPlanner.Tests.Application.Common.Behaviors
{
    public class ValidationBehaviorTests
    {
        private readonly Mock<IValidator<TestRequest>> _mockValidator;
        private readonly ValidationBehavior<TestRequest, TestResponse> _behavior;

        public ValidationBehaviorTests()
        {
            _mockValidator = new Mock<IValidator<TestRequest>>();
            var validators = new List<IValidator<TestRequest>> { _mockValidator.Object };
            _behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsNext()
        {
            // Arrange
            var request = new TestRequest { Name = "Valid" };
            var response = new TestResponse { Result = "Success" };

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Act
            var result = await _behavior.Handle(request,
                () => Task.FromResult(response),
                CancellationToken.None);

            // Assert
            Assert.Equal(response, result);
        }

        [Fact]
        public async Task Handle_InvalidRequest_ThrowsValidationException()
        {
            // Arrange
            var request = new TestRequest { Name = "" };
            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Name", "Name is required")
            };

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _behavior.Handle(request,
                    () => Task.FromResult(new TestResponse()),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NoValidators_CallsNext()
        {
            // Arrange
            var behavior = new ValidationBehavior<TestRequest, TestResponse>(new List<IValidator<TestRequest>>());
            var request = new TestRequest { Name = "Test" };
            var response = new TestResponse { Result = "Success" };

            // Act
            var result = await behavior.Handle(request,
                () => Task.FromResult(response),
                CancellationToken.None);

            // Assert
            Assert.Equal(response, result);
        }

        public class TestRequest : IRequest<TestResponse>
        {
            public string Name { get; set; } = string.Empty;
        }

        public class TestResponse
        {
            public string Result { get; set; } = string.Empty;
        }
    }
}
