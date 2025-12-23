using System.ComponentModel.DataAnnotations;

namespace SmartPlanner.Application.Auth.Dtos
{
    public record RegisterDto(
        [Required, EmailAddress, MaxLength(200)] string Email,
        [Required, MinLength(3), MaxLength(50)] string Username,
        [Required, MinLength(8)] string Password,
        [Required] string ConfirmPassword,
        string? FirstName = null,
        string? LastName = null,
        DateTime? DateOfBirth = null,
        string? PhoneNumber = null);


    public record LoginDto(
        [Required] string EmailOrUsername,
        [Required] string Password,
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
        [Required] string AccessToken,
        [Required] string RefreshToken);

    public record ForgotPasswordDto(
        [Required, EmailAddress] string Email);

    public record ResetPasswordDto(
        [Required] string Token,
        [Required, MinLength(8)] string NewPassword,
        [Required] string ConfirmNewPassword);

    public record ChangePasswordDto(
        [Required] string CurrentPassword,
        [Required, MinLength(8)] string NewPassword,
        [Required] string ConfirmNewPassword);

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
        [Required] string RefreshToken);
    public record RevokeAllTokensRequestDto(
        [Required] string Password);


    public record ConfirmEmailDto(
        [Required] Guid UserId,
        [Required] string Token);

    public record EmailConfirmationResponseDto(
        bool Success,
        string Message,
        string? RedirectUrl = null);
}
