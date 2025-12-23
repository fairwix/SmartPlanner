using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Auth.Validators;
using SmartPlanner.Application.Common.Interfaces;
using Xunit;

namespace SmartPlanner.Application.Tests.Auth
{
    public class ForgotPasswordDtoValidatorTests
    {
        private readonly ForgotPasswordDtoValidator _validator;

        public ForgotPasswordDtoValidatorTests()
        {
            _validator = new ForgotPasswordDtoValidator();
        }

        [Theory]
        [InlineData(null, false, "Email is required")]
        [InlineData("", false, "Email is required")]
        [InlineData("invalid-email", false, "Valid email address is required")]
        [InlineData("a@b.c", true, null)] // Минимально валидный email
        [InlineData("test@example.com", true, null)]
        public void Validate_Email_ShouldReturnCorrectResult(
            string email, bool expectedIsValid, string expectedErrorMessage)
        {
            // Arrange
            var dto = new ForgotPasswordDto(email);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            if (expectedIsValid)
            {
                result.ShouldNotHaveValidationErrorFor(x => x.Email);
            }
            else
            {
                result.ShouldHaveValidationErrorFor(x => x.Email)
                    .WithErrorMessage(expectedErrorMessage);
            }
        }

        [Fact]
        public void Validate_EmailTooLong_ShouldFail()
        {
            // Arrange
            var longEmail = new string('a', 190) + "@example.com"; // > 200 символов
            var dto = new ForgotPasswordDto(longEmail);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email cannot exceed 200 characters");
        }
    }

    public class LoginDtoValidatorTests
    {
        private readonly LoginDtoValidator _validator;

        public LoginDtoValidatorTests()
        {
            _validator = new LoginDtoValidator();
        }

        [Theory]
        [InlineData(null, "password", false, "Email or username is required")]
        [InlineData("", "password", false, "Email or username is required")]
        [InlineData("test@example.com", null, false, "Password is required")]
        [InlineData("test@example.com", "", false, "Password is required")]
        [InlineData("test@example.com", "password", true, null)]
        [InlineData("username", "password", true, null)]
        public void Validate_LoginDto_ShouldReturnCorrectResult(
            string emailOrUsername, string password, bool expectedIsValid, string expectedErrorMessage)
        {
            // Arrange
            var dto = new LoginDto(emailOrUsername, password);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            if (expectedIsValid)
            {
                result.ShouldNotHaveValidationErrorFor(x => x.EmailOrUsername);
                result.ShouldNotHaveValidationErrorFor(x => x.Password);
            }
            else
            {
                if (string.IsNullOrEmpty(emailOrUsername))
                {
                    result.ShouldHaveValidationErrorFor(x => x.EmailOrUsername)
                        .WithErrorMessage(expectedErrorMessage);
                }
                else
                {
                    result.ShouldHaveValidationErrorFor(x => x.Password)
                        .WithErrorMessage(expectedErrorMessage);
                }
            }
        }
    }

    public class ChangePasswordDtoValidatorTests
    {
        private readonly ChangePasswordDtoValidator _validator;

        public ChangePasswordDtoValidatorTests()
        {
            _validator = new ChangePasswordDtoValidator();
        }

        [Fact]
        public void Validate_ValidData_ShouldPass()
        {
            // Arrange
            var dto = new ChangePasswordDto("CurrentPass123!", "NewPass123!", "NewPass123!");

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null, "NewPass123!", "NewPass123!", "Current password is required")]
        [InlineData("", "NewPass123!", "NewPass123!", "Current password is required")]
        public void Validate_CurrentPassword_ShouldFailWhenEmpty(
            string currentPassword, string newPassword, string confirmPassword, string expectedErrorMessage)
        {
            // Arrange
            var dto = new ChangePasswordDto(currentPassword, newPassword, confirmPassword);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CurrentPassword)
                .WithErrorMessage(expectedErrorMessage);
        }

        [Theory]
        [InlineData("CurrentPass123!", "short", "short", "New password must be at least 8 characters")]
        [InlineData("CurrentPass123!", "nouppercase1!", "nouppercase1!", "New password must contain at least one uppercase letter")]
        [InlineData("CurrentPass123!", "NOLOWERCASE1!", "NOLOWERCASE1!", "New password must contain at least one lowercase letter")]
        [InlineData("CurrentPass123!", "NoNumbers!", "NoNumbers!", "New password must contain at least one number")]
        public void Validate_NewPasswordRules_ShouldFailWhenInvalid(
            string currentPassword, string newPassword, string confirmPassword, string expectedErrorMessage)
        {
            // Arrange
            var dto = new ChangePasswordDto(currentPassword, newPassword, confirmPassword);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage(expectedErrorMessage);
        }

        [Fact]
        public void Validate_NewPasswordSameAsCurrent_ShouldFail()
        {
            // Arrange
            var samePassword = "SamePass123!";
            var dto = new ChangePasswordDto(samePassword, samePassword, samePassword);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("New password must be different from current password");
        }

        [Fact]
        public void Validate_PasswordsDoNotMatch_ShouldFail()
        {
            // Arrange
            var dto = new ChangePasswordDto("CurrentPass123!", "NewPass123!", "Different123!");

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword)
                .WithErrorMessage("Passwords do not match");
        }
    }

    public class RegisterDtoValidatorTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<DbSet<Domain.Entities.User>> _mockUsersDbSet;
        private readonly RegisterDtoValidator _validator;
        private readonly List<Domain.Entities.User> _users;

        public RegisterDtoValidatorTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockUsersDbSet = new Mock<DbSet<Domain.Entities.User>>();
            _users = new List<Domain.Entities.User>();

            // Настройка моков для DbSet
            _mockUsersDbSet.As<IQueryable<Domain.Entities.User>>()
                .Setup(m => m.Provider)
                .Returns(_users.AsQueryable().Provider);
            _mockUsersDbSet.As<IQueryable<Domain.Entities.User>>()
                .Setup(m => m.Expression)
                .Returns(_users.AsQueryable().Expression);
            _mockUsersDbSet.As<IQueryable<Domain.Entities.User>>()
                .Setup(m => m.ElementType)
                .Returns(_users.AsQueryable().ElementType);
            _mockUsersDbSet.As<IQueryable<Domain.Entities.User>>()
                .Setup(m => m.GetEnumerator())
                .Returns(_users.AsQueryable().GetEnumerator());

            _mockContext.Setup(c => c.Users).Returns(_mockUsersDbSet.Object);

            _validator = new RegisterDtoValidator(_mockContext.Object);
        }

        [Fact]
        public async Task Validate_ValidData_ShouldPass()
        {
            // Arrange
            var dto = new RegisterDto(
                "test@example.com",
                "testuser",
                "ValidPass123!",
                "ValidPass123!",
                "John",
                "Doe",
                new DateTime(1990, 1, 1),
                "+1234567890");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null, "Email is required")]
        [InlineData("", "Email is required")]
        [InlineData("invalid-email", "Valid email address is required")]
        public async Task Validate_EmailRules_ShouldFailWhenInvalid(string email, string expectedErrorMessage)
        {
            // Arrange
            var dto = new RegisterDto(email, "testuser", "Password123!", "Password123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage(expectedErrorMessage);
        }

        [Fact]
        public async Task Validate_EmailAlreadyExists_ShouldFail()
        {
            // Arrange
            var existingEmail = "existing@example.com";
            _users.Add(new Domain.Entities.User { Email = existingEmail });

            var dto = new RegisterDto(existingEmail, "testuser", "Password123!", "Password123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is already registered");
        }

        [Theory]
        [InlineData(null, "Username is required")]
        [InlineData("", "Username is required")]
        [InlineData("ab", "Username must be at least 3 characters")] // слишком короткий
        [InlineData("user@name", "Username can only contain letters, numbers, and underscores")] // недопустимые символы
        public async Task Validate_UsernameRules_ShouldFailWhenInvalid(string username, string expectedErrorMessage)
        {
            // Arrange
            var dto = new RegisterDto("test@example.com", username, "Password123!", "Password123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage(expectedErrorMessage);
        }

        [Fact]
        public async Task Validate_UsernameAlreadyExists_ShouldFail()
        {
            // Arrange
            var existingUsername = "existinguser";
            _users.Add(new Domain.Entities.User { Username = existingUsername });

            var dto = new RegisterDto("test@example.com", existingUsername, "Password123!", "Password123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Username is already taken");
        }

        [Theory]
        [InlineData(null, "Password is required")]
        [InlineData("", "Password is required")]
        [InlineData("short", "Password must be at least 8 characters")]
        [InlineData("nouppercase1!", "Password must contain at least one uppercase letter")]
        [InlineData("NOLOWERCASE1!", "Password must contain at least one lowercase letter")]
        [InlineData("NoNumbers!", "Password must contain at least one number")]
        [InlineData("NoSpecial123", "Password must contain at least one special character")]
        public async Task Validate_PasswordRules_ShouldFailWhenInvalid(string password, string expectedErrorMessage)
        {
            // Arrange
            var dto = new RegisterDto("test@example.com", "testuser", password, password ?? "");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage(expectedErrorMessage);
        }

        [Fact]
        public async Task Validate_PasswordsDoNotMatch_ShouldFail()
        {
            // Arrange
            var dto = new RegisterDto("test@example.com", "testuser", "Password123!", "Different123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
                .WithErrorMessage("Passwords do not match");
        }

        [Theory]
        [InlineData("2008-01-01", false)] // Меньше 18 лет
        [InlineData("1990-01-01", true)] // Больше 18 лет
        public async Task Validate_DateOfBirth_ShouldCheckAge(string dateString, bool expectedIsValid)
        {
            // Arrange
            var dateOfBirth = DateTime.Parse(dateString);
            var dto = new RegisterDto("test@example.com", "testuser", "Password123!", "Password123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            if (expectedIsValid)
            {
                result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
            }
            else
            {
                result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
                    .WithErrorMessage("You must be at least 18 years old");
            }
        }

        [Theory]
        [InlineData("invalid-phone", false)]
        [InlineData("123456", false)]
        [InlineData("+1234567890", true)]
        [InlineData("1234567890", true)]
        [InlineData(null, true)] // null разрешён
        [InlineData("", true)] // пустая строка разрешена
        public async Task Validate_PhoneNumber_ShouldCheckFormat(string phoneNumber, bool expectedIsValid)
        {
            // Arrange
            var dto = new RegisterDto("test@example.com", "testuser", "Password123!", "Password123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            if (expectedIsValid)
            {
                result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
            }
            else
            {
                result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
                    .WithErrorMessage("Invalid phone number format");
            }
        }

        [Fact]
        public async Task Validate_NameLength_ShouldFailWhenTooLong()
        {
            // Arrange
            var longName = new string('a', 101); // 101 символов
            var dto = new RegisterDto("test@example.com", "testuser", "Password123!", "Password123!");

            // Act
            var result = await _validator.TestValidateAsync(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName)
                .WithErrorMessage("First name cannot exceed 100 characters");
            result.ShouldHaveValidationErrorFor(x => x.LastName)
                .WithErrorMessage("Last name cannot exceed 100 characters");
        }
    }

    public class ResetPasswordDtoValidatorTests
    {
        private readonly ResetPasswordDtoValidator _validator;

        public ResetPasswordDtoValidatorTests()
        {
            _validator = new ResetPasswordDtoValidator();
        }

        [Fact]
        public void Validate_ValidData_ShouldPass()
        {
            // Arrange
            var dto = new ResetPasswordDto("valid-token", "NewPass123!", "NewPass123!");

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null, "Token is required")]
        [InlineData("", "Token is required")]
        public void Validate_Token_ShouldFailWhenEmpty(string token, string expectedErrorMessage)
        {
            // Arrange
            var dto = new ResetPasswordDto(token, "NewPass123!", "NewPass123!");

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Token)
                .WithErrorMessage(expectedErrorMessage);
        }

        [Theory]
        [InlineData("short", "New password must be at least 8 characters")]
        [InlineData("nouppercase1!", "New password must contain at least one uppercase letter")]
        [InlineData("NOLOWERCASE1!", "New password must contain at least one lowercase letter")]
        [InlineData("NoNumbers!", "New password must contain at least one number")]
        [InlineData("NoSpecial123", "New password must contain at least one special character")]
        public void Validate_NewPasswordRules_ShouldFailWhenInvalid(
            string newPassword, string expectedErrorMessage)
        {
            // Arrange
            var dto = new ResetPasswordDto("token", newPassword, newPassword);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage(expectedErrorMessage);
        }

        [Fact]
        public void Validate_PasswordsDoNotMatch_ShouldFail()
        {
            // Arrange
            var dto = new ResetPasswordDto("token", "NewPass123!", "Different123!");

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword)
                .WithErrorMessage("Passwords do not match");
        }
    }

    public class PasswordHasherTests
    {
        private readonly PasswordHasher _passwordHasher;

        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Theory]
        [InlineData("Password123!")]
        [InlineData("AnotherSecurePass456@")]
        [InlineData("SimplePass789")]
        public void HashPassword_ShouldGenerateValidHash(string password)
        {
            // Arrange & Act
            var (hash, salt) = _passwordHasher.HashPassword(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            salt.Should().NotBeNullOrEmpty();
            hash.Should().NotBe(password); // Хеш не должен быть равен паролю
        }

        [Theory]
        [InlineData("Password123!")]
        [InlineData("AnotherSecurePass456@")]
        [InlineData("SimplePass789")]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue(string password)
        {
            // Arrange
            var (hash, salt) = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(password, hash, salt);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var originalPassword = "CorrectPass123!";
            var wrongPassword = "WrongPass456!";
            var (hash, salt) = _passwordHasher.HashPassword(originalPassword);

            // Act
            var result = _passwordHasher.VerifyPassword(wrongPassword, hash, salt);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HashPassword_TwoCallsWithSamePassword_ShouldGenerateDifferentHashes()
        {
            // Arrange
            var password = "SamePassword123!";

            // Act
            var (hash1, salt1) = _passwordHasher.HashPassword(password);
            var (hash2, salt2) = _passwordHasher.HashPassword(password);

            // Assert
            hash1.Should().NotBe(hash2); // Разные хеши из-за разных солей
            salt1.Should().NotBe(salt2);
        }

        [Fact]
        public void HashAndVerify_WithEmptyPassword_ShouldWork()
        {
            // Arrange
            var emptyPassword = "";

            // Act
            var (hash, salt) = _passwordHasher.HashPassword(emptyPassword);
            var result = _passwordHasher.VerifyPassword(emptyPassword, hash, salt);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HashAndVerify_WithVeryLongPassword_ShouldWork()
        {
            // Arrange
            var longPassword = new string('a', 1000) + "A1!";

            // Act
            var (hash, salt) = _passwordHasher.HashPassword(longPassword);
            var result = _passwordHasher.VerifyPassword(longPassword, hash, salt);

            // Assert
            result.Should().BeTrue();
        }
    }
}
