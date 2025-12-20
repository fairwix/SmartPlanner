using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Auth.Commands;
using SmartPlanner.Application.Auth.Dtos;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IMediator mediator, ILogger<AuthController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<ActionResult<AuthResponseDto>> Register(
            [FromBody] RegisterDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            var command = new RegisterCommand
            {
                Email = request.Email,
                Username = request.Username,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = request.PhoneNumber
            };

            var response = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetProfile), new { }, response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<ActionResult<AuthResponseDto>> Login(
            [FromBody] LoginDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Login attempt for: {Identifier}", request.EmailOrUsername);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var command = new LoginCommand
            {
                EmailOrUsername = request.EmailOrUsername,
                Password = request.Password,
                DeviceInfo = userAgent,
                IpAddress = ipAddress
            };

            var response = await _mediator.Send(command, cancellationToken);

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize] // ✅ Требуется аутентификация
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Logout request for user {UserId}", userId);

            var command = new LogoutCommand(userId);
            await _mediator.Send(command, cancellationToken);

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<EmailConfirmationResponseDto>> ConfirmEmail(
            [FromQuery] Guid userId,
            [FromQuery] string token,
            CancellationToken ct = default)
        {
            var command = new ConfirmEmailCommand { UserId = userId, Token = token };
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<IActionResult> Revoke(
            [FromBody] RevokeTokenRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Revoking refresh token for user {UserId}", userId);

            var command = new RevokeTokenCommand
            {
                UserId = userId,
                RefreshToken = request.RefreshToken
            };

            var success = await _mediator.Send(command, cancellationToken);
            if (!success)
            {
                return BadRequest(new { error = "Invalid refresh token." });
            }

            return Ok(new { message = "Token revoked successfully." });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<AuthResponseDto>> Refresh(
            [FromBody] RefreshTokenDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Token refresh request");

            var command = new RefreshTokenCommand
            {
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken
            };

            var response = await _mediator.Send(command, cancellationToken);
            return Ok(response);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var command = new ForgotPasswordCommand
            {
                Email = request.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            // Всегда возвращаем 200 даже если email не найден (security best practice)
            await _mediator.Send(command, cancellationToken);
            return Ok(new { message = "If your email exists in our system, you will receive a password reset link." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Password reset attempt");

            var command = new ResetPasswordCommand
            {
                Token = request.Token,
                NewPassword = request.NewPassword,
                ConfirmNewPassword = request.ConfirmNewPassword
            };

            var success = await _mediator.Send(command, cancellationToken);
            if (!success)
            {
                return Unauthorized(new { error = "Invalid or expired reset token." });
            }

            return Ok(new { message = "Password has been successfully reset." });
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 403)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordDto request,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Password change request for user {UserId}", userId);

            var command = new ChangePasswordCommand
            {
                UserId = userId,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword,
                ConfirmNewPassword = request.ConfirmNewPassword
            };

            var success = await _mediator.Send(command, cancellationToken);
            if (!success)
            {
                return BadRequest(new { error = "Current password is incorrect or new password is invalid." });
            }

            return Ok(new { message = "Password changed successfully." });
        }


        [HttpGet("profile")]
        [Authorize] // ✅ Требуется аутентификация
        [ProducesResponseType(typeof(UserProfileDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserProfileDto>> GetProfile(
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            // ✅ Можно добавить загрузку полного профиля из БД
            // var query = new GetUserProfileQuery { UserId = Guid.Parse(userIdClaim) };
            // var profile = await _mediator.Send(query, cancellationToken);

            return Ok(new UserProfileDto(
                Guid.Parse(userIdClaim),
                User.FindFirst("email")?.Value ?? "",
                User.FindFirst("username")?.Value ?? "",
                User.FindFirst(ClaimTypes.GivenName)?.Value,
                User.FindFirst(ClaimTypes.Surname)?.Value,
                null,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow,
                User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                User.FindAll("permission").Select(c => c.Value).ToList()
            ));
        }
    }
}
