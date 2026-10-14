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

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive && !u.IsDeleted,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset attempt for non-existent email: {Email}", request.Email);
            await Task.Delay(1000, cancellationToken);
            return true;
        }

        var recentRequests = await _context.PasswordResetTokens
            .CountAsync(t =>
                t.UserId == user.Id &&
                t.CreatedAt > DateTime.UtcNow.AddHours(-1),
                cancellationToken);

        if (recentRequests >= 3)
        {
            _logger.LogWarning("Too many password reset requests for user {UserId}", user.Id);
            return true;
        }

        var token = await _tokenService.GeneratePasswordResetTokenAsync(user.Id, cancellationToken);

        var baseUrl = "https://your-frontend-app.com";
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&userId={user.Id}";

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, user.Username, resetLink);
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        _logger.LogInformation(
            "Password reset initiated for user {UserId} from IP {IpAddress}",
            user.Id, request.IpAddress);

        return true;
    }
}
