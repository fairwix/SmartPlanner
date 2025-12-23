// Tests/Application/Users/Commands/CreateUserCommandValidatorTests.cs
using FluentAssertions;
using FluentValidation.TestHelper;
using SmartPlanner.Application.Users.Commands;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Users.Commands;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _validator = new CreateUserCommandValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyUsername_ShouldHaveError(string username)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = username,
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required");
    }

    [Theory]
    [InlineData("ab")] // too short
    [InlineData("a")] // too short
    [InlineData("")] // empty
    public void Validate_UsernameTooShort_ShouldHaveError(string username)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = username,
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be between 3 and 50 characters");
    }

    [Fact]
    public void Validate_UsernameTooLong_ShouldHaveError()
    {
        // Arrange
        var longUsername = new string('a', 51);
        var command = new CreateUserCommand
        {
            Username = longUsername,
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be between 3 and 50 characters");
    }

    [Theory]
    [InlineData("user name")] // space
    [InlineData("user@name")] // @ symbol
    [InlineData("user-name")] // dash
    [InlineData("user.name")] // dot
    public void Validate_UsernameInvalidCharacters_ShouldHaveError(string username)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = username,
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers, and underscores");
    }

    [Theory]
    [InlineData("valid_user")]
    [InlineData("ValidUser123")]
    [InlineData("user_123")]
    [InlineData("TEST_USER")]
    public void Validate_ValidUsername_ShouldNotHaveError(string username)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = username,
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyEmail_ShouldHaveError(string email)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = email,
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    [InlineData("user@domain")]
    public void Validate_InvalidEmailFormat_ShouldHaveError(string email)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = email,
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Valid email address is required");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user+tag@example.co.uk")]
    [InlineData("user123@test-domain.org")]
    public void Validate_ValidEmail_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = email,
            Password = "Password123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyPassword_ShouldHaveError(string password)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("12345")]
    [InlineData("abcde")]
    public void Validate_PasswordTooShort_ShouldHaveError(string password)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters");
    }

    [Theory]
    [InlineData("lowercase123")] // no uppercase
    [InlineData("UPPERCASE123")] // no lowercase
    [InlineData("NoNumbersHere")] // no numbers
    public void Validate_PasswordMissingRequirements_ShouldHaveError(string password)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ShouldHaveSpecificError()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = "lowercase123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter");
    }

    [Fact]
    public void Validate_PasswordMissingLowercase_ShouldHaveSpecificError()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = "UPPERCASE123"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one lowercase letter");
    }

    [Fact]
    public void Validate_PasswordMissingNumber_ShouldHaveSpecificError()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = "NoNumbers"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one number");
    }

    [Theory]
    [InlineData("Valid123")]
    [InlineData("PASSWORD123")]
    [InlineData("Password123")]
    [InlineData("Test123456")]
    public void Validate_ValidPassword_ShouldNotHaveError(string password)
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_AllFieldsValid_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Username = "valid_user123",
            Email = "test.user+tag@example.co.uk",
            Password = "ValidPassword123",
            Interests = new List<string> { "Programming", "Music" }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
