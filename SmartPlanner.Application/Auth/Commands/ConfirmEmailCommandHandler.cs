using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Models;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Commands
{
    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, EmailConfirmationResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IConfirmationTokenService _tokenService;
        private readonly ILogger<ConfirmEmailCommandHandler> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly AppSettings _appSettings;

        public ConfirmEmailCommandHandler(
            IApplicationDbContext context,
            IConfirmationTokenService tokenService,
            ILogger<ConfirmEmailCommandHandler> logger,
            IEmailService emailService,
            IAuditService auditService,
            IOptions<AppSettings> appSettings)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
            _emailService = emailService;
            _auditService = auditService;
            _appSettings = appSettings.Value;
        }

        public async Task<EmailConfirmationResponseDto> Handle(
            ConfirmEmailCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email confirmation attempt for user {UserId}", request.UserId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive && !u.IsDeleted,
                    cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found or inactive", request.UserId);
                await _auditService.LogSecurityEventAsync(
                    SecurityEventType.EmailConfirmed,
                    request.UserId,
                    success: false,
                    details: new { Reason = "User not found" },
                    cancellationToken: cancellationToken);

                return new EmailConfirmationResponseDto(false, "User not found or account is inactive");
            }

            if (user.IsEmailConfirmed)
            {
                _logger.LogInformation("Email already confirmed for user {UserId}", user.Id);
                return new EmailConfirmationResponseDto(
                    true,
                    "Email already confirmed",
                    _appSettings.FrontendUrls?.LoginUrl ?? "/login");
            }

            var isValid = await _tokenService.ValidateEmailConfirmationTokenAsync(
                request.Token, user.Id, cancellationToken);

            if (!isValid)
            {
                _logger.LogWarning("Invalid or expired confirmation token for user {UserId}", user.Id);

                await _auditService.LogSecurityEventAsync(
                    SecurityEventType.EmailConfirmed,
                    user.Id,
                    user.Email,
                    success: false,
                    details: new { Reason = "Invalid token" },
                    cancellationToken: cancellationToken);

                return new EmailConfirmationResponseDto(
                    false,
                    "Invalid or expired confirmation token. Please request a new one.");
            }

            await MarkEmailTokenAsUsedAsync(request.Token, cancellationToken);

            user.IsEmailConfirmed = true;
            user.EmailConfirmedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Email successfully confirmed for user {UserId}", user.Id);

            await _auditService.LogSecurityEventAsync(
                SecurityEventType.EmailConfirmed,
                user.Id,
                user.Email,
                success: true,
                cancellationToken: cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                    _logger.LogDebug("Welcome email sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }
            }, cancellationToken);

            return new EmailConfirmationResponseDto(
                true,
                "Email successfully confirmed! You can now log in.",
                _appSettings.FrontendUrls?.DashboardUrl ?? "/dashboard");
        }

        private async Task MarkEmailTokenAsUsedAsync(string token, CancellationToken cancellationToken)
        {
            var tokenHash = ComputeSha256Hash(token);

            var confirmationToken = await _context.EmailConfirmationTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (confirmationToken != null)
            {
                confirmationToken.IsUsed = true;
                confirmationToken.UsedAt = DateTime.UtcNow;
            }
        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
