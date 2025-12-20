using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfirmationTokenService _tokenService;
    private readonly ITokenService _authTokenService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;
    private readonly IEmailService _emailService; // ✅ ДОБАВИТЬ

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfirmationTokenService tokenService,
        ITokenService authTokenService,
        ILogger<ResetPasswordCommandHandler> logger,
        IEmailService emailService) // ✅ ДОБАВИТЬ параметр
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _authTokenService = authTokenService;
        _logger = logger;
        _emailService = emailService; // ✅ ИНИЦИАЛИЗИРОВАТЬ
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt from IP: {IpAddress}", request.IpAddress);

        // 1. Извлекаем userId из токена (нужно расширить IConfirmationTokenService)
        var userId = await ExtractUserIdFromTokenAsync(request.Token, cancellationToken);
        if (!userId.HasValue)
        {
            _logger.LogWarning("Invalid or expired password reset token");
            return false;
        }

        // 2. Валидируем токен
        var isValid = await _tokenService.ValidatePasswordResetTokenAsync(
            request.Token, userId.Value, cancellationToken);

        if (!isValid)
        {
            _logger.LogWarning("Invalid password reset token for user {UserId}", userId);
            return false;
        }

        // 3. Находим пользователя
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive && !u.IsDeleted,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found or inactive", userId);
            return false;
        }

        // 4. Проверяем, что новый пароль не совпадает со старым
        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("New password must be different from current password for user {UserId}", user.Id);
            return false;
        }

        // 5. Хешируем новый пароль
        var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.NewPassword);

        // 6. Обновляем пароль
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.UpdatedAt = DateTime.UtcNow;

        // 7. Отзываем токен сброса пароля
        await _tokenService.RevokePasswordResetTokenAsync(request.Token, cancellationToken);

        // 8. Отзываем ВСЕ сессии пользователя (по требованию ТЗ)
        await _authTokenService.RevokeUserSessionsAsync(user.Id, cancellationToken);

        // 9. Сохраняем изменения
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

        // 10. Отправляем confirmation email (если emailService доступен)
        try
        {
            if (_emailService != null)
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Password Changed - Smart Planner",
                    $"<p>Your password was successfully changed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.</p>" +
                    "<p>If you didn't make this change, please contact support immediately.</p>");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password change confirmation to {Email}", user.Email);
            // Не прерываем выполнение из-за ошибки отправки email
        }

        return true;
    }

    private async Task<Guid?> ExtractUserIdFromTokenAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeSha256Hash(token);

        var resetToken = await _context.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        return resetToken?.UserId;
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
