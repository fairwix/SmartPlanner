// SmartPlanner.Tests/Application/Auth/Validators/RegisterDtoValidatorTests.cs

using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Validators;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Tests.TestHelpers;
using Xunit;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartPlanner.Tests.Application.Auth.Validators;

public class RegisterDtoValidatorTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;

    public RegisterDtoValidatorTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        var users = MockDbSetHelper.CreateMockDbSet(new List<User>());
        _mockContext.Setup(c => c.Users).Returns(users.Object);
    }

    [Fact]
    public async Task Validate_ValidDto_PassesValidation()
    {
        // Arrange
        var dto = new RegisterDto("test@test.com", "user123", "Pass123!", "Pass123!");
        var validator = new RegisterDtoValidator(_mockContext.Object);

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_PasswordTooShort_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto("test@test.com", "user123", "short", "short");
        var validator = new RegisterDtoValidator(_mockContext.Object);

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters");
    }

    [Theory]
    [InlineData("test@test", "username", "Password123", "Password123")] // Invalid email
    [InlineData("test@test.com", "us", "Password123", "Password123")] // Username too short
    [InlineData("test@test.com", "username", "password", "password")] // No uppercase in password
    [InlineData("test@test.com", "username", "PASSWORD123", "PASSWORD123")] // No lowercase in password
    [InlineData("test@test.com", "username", "Pass123", "DifferentPass123")] // Passwords don't match
    public async Task Validate_InvalidDto_FailsValidation(string email, string username, string password, string confirmPassword)
    {
        // Arrange
        var dto = new RegisterDto(email, username, password, confirmPassword);
        var validator = new RegisterDtoValidator(_mockContext.Object);

        // Act
        var result = await validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveAnyValidationError();
    }
}
