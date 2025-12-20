using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Auth.Services;

namespace SmartPlanner.Application.Auth.Commands;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfirmationTokenService _tokenService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IConfirmationTokenService tokenService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

        // 1. Находим пользователя (используем AsNoTracking для безопасности)
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive && !u.IsDeleted,
                cancellationToken);

        // 2. Всегда возвращаем true для безопасности (не раскрываем существование email)
        if (user == null)
        {
            _logger.LogWarning("Password reset attempt for non-existent email: {Email}", request.Email);
            await Task.Delay(1000, cancellationToken); // Задержка для защиты от timing attacks
            return true;
        }

        // 3. Проверяем частоту запросов (защита от спама)
        var recentRequests = await _context.PasswordResetTokens
            .CountAsync(t =>
                t.UserId == user.Id &&
                t.CreatedAt > DateTime.UtcNow.AddHours(-1),
                cancellationToken);

        if (recentRequests >= 3)
        {
            _logger.LogWarning("Too many password reset requests for user {UserId}", user.Id);
            return true; // Все равно возвращаем true
        }

        // 4. Генерируем токен
        var token = await _tokenService.GeneratePasswordResetTokenAsync(user.Id, cancellationToken);

        // 5. Формируем ссылку для сброса
        var baseUrl = "https://your-frontend-app.com"; // Взять из конфигурации
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&userId={user.Id}";

        // 6. Отправляем email
        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, user.Username, resetLink);
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            // Можно сохранить в очередь для повторной отправки
        }

        // 7. Логируем событие безопасности
        _logger.LogInformation(
            "Password reset initiated for user {UserId} from IP {IpAddress}",
            user.Id, request.IpAddress);

        return true;
    }
}
