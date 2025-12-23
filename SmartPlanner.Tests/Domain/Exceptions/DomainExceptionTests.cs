// SmartPlanner.Tests/Domain/Exceptions/DomainExceptionTests.cs
using SmartPlanner.Domain.Exceptions;
using Xunit;
using FluentAssertions;

namespace SmartPlanner.Tests.Domain.Exceptions
{
    public class DomainExceptionTests
    {
        [Fact]
        public void DomainException_WithMessage_CreatesException()
        {
            // Arrange & Act
            var exception = new DomainException("Test message");

            // Assert
            exception.Message.Should().Be("Test message");
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void DomainException_WithMessageAndInnerException_CreatesException()
        {
            // Arrange
            var innerException = new System.Exception("Inner exception");

            // Act
            var exception = new DomainException("Test message", innerException);

            // Assert
            exception.Message.Should().Be("Test message");
            exception.InnerException.Should().Be(innerException);
        }

        [Fact]
        public void DomainValidationException_WithMessage_CreatesException()
        {
            // Arrange & Act
            var exception = new DomainValidationException("Validation failed");

            // Assert
            exception.Message.Should().Be("Validation failed");
            exception.InnerException.Should().BeNull();
        }
    }
}
