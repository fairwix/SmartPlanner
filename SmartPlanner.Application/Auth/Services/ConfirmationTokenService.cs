using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using SmartPlanner.Application.Auth.Interfaces;

namespace SmartPlanner.Application.Auth.Services;

public class ConfirmationTokenService : IConfirmationTokenService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ConfirmationTokenService> _logger;
    private readonly IConfiguration _configuration;

    public ConfirmationTokenService(
        IApplicationDbContext context,
        ILogger<ConfirmationTokenService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task MarkEmailTokenAsUsedAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeSha256Hash(token);

        var confirmationToken = await _context.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (confirmationToken != null)
        {
            confirmationToken.IsUsed = true;
            confirmationToken.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(24); // 24 часа как в ТЗ

        var passwordResetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = ComputeSha256Hash(token),
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await _context.PasswordResetTokens.AddAsync(passwordResetToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated password reset token for user {UserId}", userId);
        return token;
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var emailConfirmationToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = ComputeSha256Hash(token),
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await _context.EmailConfirmationTokens.AddAsync(emailConfirmationToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated email confirmation token for user {UserId}", userId);
        return token;
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token, Guid userId, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeSha256Hash(token);

        var resetToken = await _context.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.TokenHash == tokenHash &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        return resetToken != null;
    }

    public async Task<bool> ValidateEmailConfirmationTokenAsync(string token, Guid userId, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeSha256Hash(token);

        var confirmationToken = await _context.EmailConfirmationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.TokenHash == tokenHash &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        return confirmationToken != null;
    }

    public async Task RevokePasswordResetTokenAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeSha256Hash(token);

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken != null)
        {
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Password reset token revoked for user {UserId}", resetToken.UserId);
        }
    }


    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private async Task<Guid?> ExtractUserIdFromTokenAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeSha256Hash(token);

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken != null)
            return resetToken.UserId;

        var emailToken = await _context.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        return emailToken?.UserId;
    }
}
