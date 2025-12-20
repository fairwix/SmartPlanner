using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;
    private readonly IEmailService _emailService; // ✅ ДОБАВИТЬ

    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<ChangePasswordCommandHandler> logger,
        IEmailService emailService) // ✅ ДОБАВИТЬ параметр
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
        _emailService = emailService; // ✅ ИНИЦИАЛИЗИРОВАТЬ
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Change password request for user {UserId}", request.UserId);

        // 1. Находим пользователя
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive && !u.IsDeleted,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found or inactive", request.UserId);
            return false;
        }

        // 2. Проверяем текущий пароль
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("Invalid current password for user {UserId}", user.Id);
            return false;
        }

        // 3. Проверяем, что новый пароль отличается от старого
        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("New password must be different from current password for user {UserId}", user.Id);
            return false;
        }

        // 4. Хешируем новый пароль
        var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.NewPassword);

        // 5. Обновляем пароль
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.UpdatedAt = DateTime.UtcNow;

        // 6. Отзываем ВСЕ сессии пользователя (по требованию ТЗ)
        await _tokenService.RevokeUserSessionsAsync(user.Id, cancellationToken);

        // 7. Сохраняем изменения
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully changed for user {UserId}", user.Id);

        // 8. Отправляем email уведомление (если emailService доступен)
        try
        {
            if (_emailService != null)
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Password Changed - Smart Planner",
                    $"<p>Your password was successfully changed on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.</p>" +
                    "<p>All your active sessions have been logged out for security.</p>");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password change notification to {Email}", user.Email);
            // Не прерываем выполнение из-за ошибки отправки email
        }

        return true;
    }
}
