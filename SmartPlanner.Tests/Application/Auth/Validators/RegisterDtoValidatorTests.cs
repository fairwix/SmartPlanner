// SmartPlanner.Tests/Application/Auth/Validators/RegisterDtoValidatorTests.cs

using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Validators;
using SmartPlanner.Infrastructure.Data;
using Xunit;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartPlanner.Tests.Application.Auth.Validators;

public class RegisterDtoValidatorTests
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly Mock<DbSet<object>> _mockDbSet;

    public RegisterDtoValidatorTests()
    {
        _mockDbSet = new Mock<DbSet<object>>();
        _mockContext = new Mock<AppDbContext>();

        // Setup mock DbSet for Users
        var usersData = new List<object>().AsQueryable();
        _mockDbSet.As<IQueryable<object>>().Setup(m => m.Provider).Returns(usersData.Provider);
        _mockDbSet.As<IQueryable<object>>().Setup(m => m.Expression).Returns(usersData.Expression);
        _mockDbSet.As<IQueryable<object>>().Setup(m => m.ElementType).Returns(usersData.ElementType);
        _mockDbSet.As<IQueryable<object>>().Setup(m => m.GetEnumerator()).Returns(usersData.GetEnumerator());

        // Setup mock context to return our mock DbSet
        _mockContext.Setup(c => c.Set<object>()).Returns(_mockDbSet.Object);
    }

    [Fact]
    public void Validate_ValidDto_PassesValidation()
    {
        // Arrange
        var dto = new RegisterDto("test@test.com", "user123", "Pass123!", "Pass123!");
        var validator = new RegisterDtoValidator(_mockContext.Object);

        // Act
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_PasswordTooShort_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto("test@test.com", "user123", "short", "short");
        var validator = new RegisterDtoValidator(_mockContext.Object);

        // Act
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters long.");
    }

    [Theory]
    [InlineData("test@test", "username", "Password123", "Password123")] // Invalid email
    [InlineData("test@test.com", "us", "Password123", "Password123")] // Username too short
    [InlineData("test@test.com", "username", "password", "password")] // No uppercase in password
    [InlineData("test@test.com", "username", "PASSWORD123", "PASSWORD123")] // No lowercase in password
    [InlineData("test@test.com", "username", "Pass123", "DifferentPass123")] // Passwords don't match
    public void Validate_InvalidDto_FailsValidation(string email, string username, string password, string confirmPassword)
    {
        // Arrange
        var dto = new RegisterDto(email, username, password, confirmPassword);
        var validator = new RegisterDtoValidator(_mockContext.Object);

        // Act
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldHaveAnyValidationError();
    }
}
