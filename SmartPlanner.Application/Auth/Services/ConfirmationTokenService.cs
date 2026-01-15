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
        var normalizedToken = NormalizeTokenForHashing(token);
        var tokenHash = ComputeSha256Hash(normalizedToken);

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
        var normalizedToken = NormalizeTokenForHashing(token);
        var tokenHash = ComputeSha256Hash(normalizedToken);

        var expiresAt = DateTime.UtcNow.AddHours(24); // 24 часа как в ТЗ

        var passwordResetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
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
        var token = GenerateSecureToken(); // URL-safe токен

        // Для хэширования используем нормализованную версию
        var normalizedToken = NormalizeTokenForHashing(token);
        var tokenHash = ComputeSha256Hash(normalizedToken);

        var emailConfirmationToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash, // Хэш от нормализованного токена
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false
        };

        await _context.EmailConfirmationTokens.AddAsync(emailConfirmationToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Получаем пользователя для email
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        var email = user?.Email ?? "unknown";

        _logger.LogInformation("Generated email confirmation token for user {UserId}", userId);

        // Выводим подробную информацию в консоль
        var confirmationUrl = $"/api/Auth/confirm-email?userId={userId}&token={Uri.EscapeDataString(token)}";
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5047";
        var fullUrl = $"{baseUrl}{confirmationUrl}";

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("📧 EMAIL CONFIRMATION TOKEN GENERATED");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"👤 User ID: {userId}");
        Console.WriteLine($"📧 Email: {email}");
        Console.WriteLine($"🔑 Token: {token}");
        Console.WriteLine($"🔗 Confirmation URL: {fullUrl}");
        Console.WriteLine(new string('=', 60) + "\n");
        Console.ResetColor();

        return token; // Возвращаем URL-safe версию
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token, Guid userId, CancellationToken cancellationToken)
    {
        var normalizedToken = NormalizeTokenForHashing(token);
        var tokenHash = ComputeSha256Hash(normalizedToken);

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
        // Нормализуем токен так же как при генерации
        var normalizedToken = NormalizeTokenForHashing(token);
        var tokenHash = ComputeSha256Hash(normalizedToken);

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
        var normalizedToken = NormalizeTokenForHashing(token);
        var tokenHash = ComputeSha256Hash(normalizedToken);

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

    private static string NormalizeTokenForHashing(string urlSafeToken)
    {
        // Конвертируем URL-safe Base64 обратно в обычный Base64
        var base64 = urlSafeToken
            .Replace('-', '+')
            .Replace('_', '/');

        // Добавляем padding если нужно
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return base64;
    }

    private async Task<Guid?> ExtractUserIdFromTokenAsync(string token, CancellationToken cancellationToken)
    {
        var normalizedToken = NormalizeTokenForHashing(token);
        var tokenHash = ComputeSha256Hash(normalizedToken);

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken != null)
            return resetToken.UserId;

        var emailToken = await _context.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        return emailToken?.UserId;
    }
}
