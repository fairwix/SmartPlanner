using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SmartPlanner.Application.Auth.Dtos;
using Xunit;

namespace SmartPlanner.Application.Tests.Auth.Dtos
{
    public class AuthDtosTests
    {
        #region RegisterDto Tests

        public class RegisterDtoTests
        {
            [Theory]
            [InlineData(null, "testuser", "Password123!", "Password123!", false)] // Email null
            [InlineData("", "testuser", "Password123!", "Password123!", false)] // Email empty
            [InlineData("invalid-email", "testuser", "Password123!", "Password123!", false)] // Invalid email
            [InlineData("test@example.com", null, "Password123!", "Password123!", false)] // Username null
            [InlineData("test@example.com", "", "Password123!", "Password123!", false)] // Username empty
            [InlineData("test@example.com", "ab", "Password123!", "Password123!", false)] // Username too short
            [InlineData("test@example.com", "testuser", null, "Password123!", false)] // Password null
            [InlineData("test@example.com", "testuser", "", "Password123!", false)] // Password empty
            [InlineData("test@example.com", "testuser", "short", "short", false)] // Password too short
            [InlineData("test@example.com", "testuser", "Password123!", null, false)] // ConfirmPassword null
            [InlineData("test@example.com", "testuser", "Password123!", "Different123!", false)] // Mismatch passwords
            [InlineData("test@example.com", "validuser", "Password123!", "Password123!", true)] // Valid data
            public void RegisterDto_Validation_ShouldBeCorrect(
                string email, string username, string password, string confirmPassword, bool expectedIsValid)
            {
                // Arrange
                var dto = new RegisterDto(
                    email,
                    username,
                    password,
                    confirmPassword,
                    "John",
                    "Doe",
                    new DateTime(1990, 1, 1),
                    "+1234567890");

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }

            [Fact]
            public void RegisterDto_WithValidData_ShouldCreateSuccessfully()
            {
                // Arrange & Act
                var dto = new RegisterDto(
                    "test@example.com",
                    "testuser",
                    "Password123!",
                    "Password123!",
                    "John",
                    "Doe",
                    new DateTime(1990, 1, 1),
                    "+1234567890");

                // Assert
                dto.Email.Should().Be("test@example.com");
                dto.Username.Should().Be("testuser");
                dto.Password.Should().Be("Password123!");
                dto.ConfirmPassword.Should().Be("Password123!");
                dto.FirstName.Should().Be("John");
                dto.LastName.Should().Be("Doe");
                dto.DateOfBirth.Should().Be(new DateTime(1990, 1, 1));
                dto.PhoneNumber.Should().Be("+1234567890");
            }

            [Fact]
            public void RegisterDto_OptionalFields_ShouldAllowNulls()
            {
                // Arrange & Act
                var dto = new RegisterDto(
                    "test@example.com",
                    "testuser",
                    "Password123!",
                    "Password123!");

                // Assert
                dto.FirstName.Should().BeNull();
                dto.LastName.Should().BeNull();
                dto.DateOfBirth.Should().BeNull();
                dto.PhoneNumber.Should().BeNull();
            }
        }

        #endregion

        #region LoginDto Tests

        public class LoginDtoTests
        {
            [Theory]
            [InlineData(null, "password", false)] // EmailOrUsername null
            [InlineData("", "password", false)] // EmailOrUsername empty
            [InlineData("test@example.com", null, false)] // Password null
            [InlineData("test@example.com", "", false)] // Password empty
            [InlineData("test@example.com", "password", true)] // Valid data
            [InlineData("username", "password", true)] // Valid with username
            public void LoginDto_Validation_ShouldBeCorrect(
                string emailOrUsername, string password, bool expectedIsValid)
            {
                // Arrange
                var dto = new LoginDto(emailOrUsername, password, "Device Info", "127.0.0.1");
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }

            [Fact]
            public void LoginDto_OptionalFields_ShouldBeIncluded()
            {
                // Arrange & Act
                var dto = new LoginDto("test@example.com", "password", "iPhone", "192.168.1.1");

                // Assert
                dto.EmailOrUsername.Should().Be("test@example.com");
                dto.Password.Should().Be("password");
                dto.DeviceInfo.Should().Be("iPhone");
                dto.IpAddress.Should().Be("192.168.1.1");
            }
        }

        #endregion

        #region AuthResponseDto Tests

        public class AuthResponseDtoTests
        {
            [Fact]
            public void AuthResponseDto_ShouldCreateSuccessfully()
            {
                // Arrange
                var userProfile = new UserProfileDto(
                    Guid.NewGuid(),
                    "test@example.com",
                    "testuser",
                    "John",
                    "Doe",
                    new DateTime(1990, 1, 1),
                    "+1234567890",
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    new List<string> { "User" },
                    new List<string> { "CanView" });

                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Act
                var dto = new AuthResponseDto(
                    "access-token",
                    "refresh-token",
                    accessTokenExpiry,
                    refreshTokenExpiry,
                    userProfile);

                // Assert
                dto.AccessToken.Should().Be("access-token");
                dto.RefreshToken.Should().Be("refresh-token");
                dto.AccessTokenExpiry.Should().Be(accessTokenExpiry);
                dto.RefreshTokenExpiry.Should().Be(refreshTokenExpiry);
                dto.User.Should().BeSameAs(userProfile);
            }
        }

        #endregion

        #region UserProfileDto Tests

        public class UserProfileDtoTests
        {
            [Fact]
            public void UserProfileDto_ShouldCreateSuccessfully()
            {
                // Arrange
                var userId = Guid.NewGuid();
                var createdAt = DateTime.UtcNow.AddDays(-10);
                var lastLoginAt = DateTime.UtcNow;
                var roles = new List<string> { "User", "Premium" };
                var permissions = new List<string> { "CanCreate", "CanEdit", "CanDelete" };

                // Act
                var dto = new UserProfileDto(
                    userId,
                    "test@example.com",
                    "testuser",
                    "John",
                    "Doe",
                    new DateTime(1990, 1, 1),
                    "+1234567890",
                    createdAt,
                    lastLoginAt,
                    roles,
                    permissions);

                // Assert
                dto.Id.Should().Be(userId);
                dto.Email.Should().Be("test@example.com");
                dto.Username.Should().Be("testuser");
                dto.FirstName.Should().Be("John");
                dto.LastName.Should().Be("Doe");
                dto.DateOfBirth.Should().Be(new DateTime(1990, 1, 1));
                dto.PhoneNumber.Should().Be("+1234567890");
                dto.CreatedAt.Should().Be(createdAt);
                dto.LastLoginAt.Should().Be(lastLoginAt);
                dto.Roles.Should().BeEquivalentTo(roles);
                dto.Permissions.Should().BeEquivalentTo(permissions);
            }

            [Fact]
            public void UserProfileDto_WithNullOptionalFields_ShouldCreateSuccessfully()
            {
                // Arrange & Act
                var dto = new UserProfileDto(
                    Guid.NewGuid(),
                    "test@example.com",
                    "testuser",
                    null,
                    null,
                    null,
                    null,
                    DateTime.UtcNow,
                    null,
                    new List<string>(),
                    new List<string>());

                // Assert
                dto.FirstName.Should().BeNull();
                dto.LastName.Should().BeNull();
                dto.DateOfBirth.Should().BeNull();
                dto.PhoneNumber.Should().BeNull();
                dto.LastLoginAt.Should().BeNull();
                dto.Roles.Should().BeEmpty();
                dto.Permissions.Should().BeEmpty();
            }
        }

        #endregion

        #region RefreshTokenDto Tests

        public class RefreshTokenDtoTests
        {
            [Theory]
            [InlineData(null, "refresh-token", false)] // AccessToken null
            [InlineData("", "refresh-token", false)] // AccessToken empty
            [InlineData("access-token", null, false)] // RefreshToken null
            [InlineData("access-token", "", false)] // RefreshToken empty
            [InlineData("access-token", "refresh-token", true)] // Valid data
            public void RefreshTokenDto_Validation_ShouldBeCorrect(
                string accessToken, string refreshToken, bool expectedIsValid)
            {
                // Arrange
                var dto = new RefreshTokenDto(accessToken, refreshToken);
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }
        }

        #endregion

        #region ForgotPasswordDto Tests

        public class ForgotPasswordDtoTests
        {
            [Theory]
            [InlineData(null, false)] // Email null
            [InlineData("", false)] // Email empty
            [InlineData("invalid-email", false)] // Invalid email
            [InlineData("test@example.com", true)] // Valid email
            public void ForgotPasswordDto_Validation_ShouldBeCorrect(string email, bool expectedIsValid)
            {
                // Arrange
                var dto = new ForgotPasswordDto(email);
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }
        }

        #endregion

        #region ResetPasswordDto Tests

        public class ResetPasswordDtoTests
        {
            [Theory]
            [InlineData(null, "NewPass123!", "NewPass123!", false)] // Token null
            [InlineData("token", null, "NewPass123!", false)] // NewPassword null
            [InlineData("token", "", "NewPass123!", false)] // NewPassword empty
            [InlineData("token", "short", "short", false)] // NewPassword too short
            [InlineData("token", "NewPass123!", null, false)] // ConfirmNewPassword null
            [InlineData("token", "NewPass123!", "Different123!", false)] // Mismatch passwords
            [InlineData("token", "NewPass123!", "NewPass123!", true)] // Valid data
            public void ResetPasswordDto_Validation_ShouldBeCorrect(
                string token, string newPassword, string confirmNewPassword, bool expectedIsValid)
            {
                // Arrange
                var dto = new ResetPasswordDto(token, newPassword, confirmNewPassword);
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }
        }

        #endregion

        #region ChangePasswordDto Tests

        public class ChangePasswordDtoTests
        {
            [Theory]
            [InlineData(null, "NewPass123!", "NewPass123!", false)] // CurrentPassword null
            [InlineData("current", null, "NewPass123!", false)] // NewPassword null
            [InlineData("current", "", "NewPass123!", false)] // NewPassword empty
            [InlineData("current", "short", "short", false)] // NewPassword too short
            [InlineData("current", "NewPass123!", null, false)] // ConfirmNewPassword null
            [InlineData("current", "NewPass123!", "Different123!", false)] // Mismatch passwords
            [InlineData("CurrentPass123!", "NewPass123!", "NewPass123!", true)] // Valid data
            public void ChangePasswordDto_Validation_ShouldBeCorrect(
                string currentPassword, string newPassword, string confirmNewPassword, bool expectedIsValid)
            {
                // Arrange
                var dto = new ChangePasswordDto(currentPassword, newPassword, confirmNewPassword);
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }
        }

        #endregion

        #region TokenResponseDto Tests

        public class TokenResponseDtoTests
        {
            [Fact]
            public void TokenResponseDto_ShouldCreateSuccessfully()
            {
                // Arrange
                var userProfile = new UserProfileDto(
                    Guid.NewGuid(),
                    "test@example.com",
                    "testuser",
                    "John",
                    "Doe",
                    null,
                    null,
                    DateTime.UtcNow,
                    null,
                    new List<string> { "User" },
                    new List<string>());

                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Act
                var dto = new TokenResponseDto(
                    "access-token",
                    "refresh-token",
                    accessTokenExpiry,
                    refreshTokenExpiry,
                    userProfile);

                // Assert
                dto.AccessToken.Should().Be("access-token");
                dto.RefreshToken.Should().Be("refresh-token");
                dto.AccessTokenExpiry.Should().Be(accessTokenExpiry);
                dto.RefreshTokenExpiry.Should().Be(refreshTokenExpiry);
                dto.User.Should().BeSameAs(userProfile);
            }
        }

        #endregion

        // Добавьте этот класс в AuthDtosTests.cs

        #region RefreshTokenRequestDto Tests

        public class RefreshTokenRequestDtoTests
        {
            [Fact]
            public void RefreshTokenRequestDto_ShouldCreateSuccessfully()
            {
                // Arrange & Act
                var dto = new RefreshTokenRequestDto("access-token", "refresh-token");

                // Assert
                dto.AccessToken.Should().Be("access-token");
                dto.RefreshToken.Should().Be("refresh-token");
            }

            [Fact]
            public void RefreshTokenRequestDto_WithDeconstruct_ShouldWork()
            {
                // Arrange & Act
                var dto = new RefreshTokenRequestDto("access-token", "refresh-token");
                var (accessToken, refreshToken) = dto;

                // Assert
                accessToken.Should().Be("access-token");
                refreshToken.Should().Be("refresh-token");
            }
        }

        #endregion

        #region RevokeTokenRequestDto Tests

        public class RevokeTokenRequestDtoTests
        {
            [Theory]
            [InlineData(null, false)] // RefreshToken null
            [InlineData("", false)] // RefreshToken empty
            [InlineData("refresh-token", true)] // Valid data
            public void RevokeTokenRequestDto_Validation_ShouldBeCorrect(
                string refreshToken, bool expectedIsValid)
            {
                // Arrange
                var dto = new RevokeTokenRequestDto(refreshToken);
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }
        }

        #endregion

        #region RevokeAllTokensRequestDto Tests

        public class RevokeAllTokensRequestDtoTests
        {
            [Theory]
            [InlineData(null, false)] // Password null
            [InlineData("", false)] // Password empty
            [InlineData("password123", true)] // Valid data
            public void RevokeAllTokensRequestDto_Validation_ShouldBeCorrect(
                string password, bool expectedIsValid)
            {
                // Arrange
                var dto = new RevokeAllTokensRequestDto(password);
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }
        }

        #endregion

        #region ConfirmEmailDto Tests

        public class ConfirmEmailDtoTests
        {
            [Theory]
            [InlineData("00000000-0000-0000-0000-000000000000", null, false)] // Empty Guid, null token
            [InlineData("00000000-0000-0000-0000-000000000000", "", false)] // Empty Guid, empty token
            [InlineData("123e4567-e89b-12d3-a456-426614174000", null, false)] // Valid Guid, null token
            [InlineData("123e4567-e89b-12d3-a456-426614174000", "", false)] // Valid Guid, empty token
            [InlineData("123e4567-e89b-12d3-a456-426614174000", "valid-token", true)] // Valid data
            public void ConfirmEmailDto_Validation_ShouldBeCorrect(
                string userIdString, string token, bool expectedIsValid)
            {
                // Arrange
                var userId = Guid.Parse(userIdString);
                var dto = new ConfirmEmailDto(userId, token);
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                // Act
                var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

                // Assert
                isValid.Should().Be(expectedIsValid);
            }
        }

        #endregion

        #region EmailConfirmationResponseDto Tests

        public class EmailConfirmationResponseDtoTests
        {
            [Theory]
            [InlineData(true, "Success")]
            [InlineData(false, "Failed")]
            public void EmailConfirmationResponseDto_ShouldCreateSuccessfully(bool success, string message)
            {
                // Arrange & Act
                var dto = new EmailConfirmationResponseDto(success, message, "https://example.com/redirect");

                // Assert
                dto.Success.Should().Be(success);
                dto.Message.Should().Be(message);
                dto.RedirectUrl.Should().Be("https://example.com/redirect");
            }

            [Fact]
            public void EmailConfirmationResponseDto_OptionalRedirectUrl_ShouldBeNull()
            {
                // Arrange & Act
                var dto = new EmailConfirmationResponseDto(true, "Success");

                // Assert
                dto.RedirectUrl.Should().BeNull();
            }
        }

        #endregion
    }
}
