using System.ComponentModel.DataAnnotations;

namespace SmartPlanner.Application.Auth.Dtos
{
    public record RegisterDto(
        [property: Required, EmailAddress, MaxLength(200)] string Email,
        [property: Required, MinLength(3), MaxLength(50)] string Username,
        [property: Required, MinLength(8)] string Password,
        [property: Required, Compare("Password")] string ConfirmPassword,
        string? FirstName = null,
        string? LastName = null,
        DateTime? DateOfBirth = null,
        string? PhoneNumber = null);


    public record LoginDto(
        [property: Required] string EmailOrUsername,
        [property: Required] string Password,
        string? DeviceInfo = null,
        string? IpAddress = null);

    public record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        DateTime RefreshTokenExpiry,
        UserProfileDto User);

    public record UserProfileDto(
        Guid Id,
        string Email,
        string Username,
        string? FirstName,
        string? LastName,
        DateTime? DateOfBirth,
        string? PhoneNumber,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        List<string> Roles,
        List<string> Permissions);

    public record RefreshTokenDto(
        [property: Required] string AccessToken,
        [property: Required] string RefreshToken);

    public record ForgotPasswordDto(
        [property: Required, EmailAddress] string Email);

    public record ResetPasswordDto(
        [property: Required] string Token,
        [property: Required, MinLength(8)] string NewPassword,
        [property: Required, Compare("NewPassword")] string ConfirmNewPassword);

    public record ChangePasswordDto(
        [property: Required] string CurrentPassword,
        [property: Required, MinLength(8)] string NewPassword,
        [property: Required, Compare("NewPassword")] string ConfirmNewPassword);

    public record TokenResponseDto(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiry,
        DateTime RefreshTokenExpiry,
        UserProfileDto User);

    public record RefreshTokenRequestDto(
        string AccessToken,
        string RefreshToken);

    public record RevokeTokenRequestDto(
        [property: Required] string RefreshToken);
    public record RevokeAllTokensRequestDto(
        [property: Required] string Password);


    public record ConfirmEmailDto(
        [property: Required] Guid UserId,
        [property: Required] string Token);

    public record EmailConfirmationResponseDto(
        bool Success,
        string Message,
        string? RedirectUrl = null);
}
