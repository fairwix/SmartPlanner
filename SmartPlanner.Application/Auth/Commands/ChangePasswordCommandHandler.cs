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
    private readonly IEmailService _emailService;

    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<ChangePasswordCommandHandler> logger,
        IEmailService emailService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Change password request for user {UserId}", request.UserId);


        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive && !u.IsDeleted,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found or inactive", request.UserId);
            return false;
        }


        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("Invalid current password for user {UserId}", user.Id);
            return false;
        }


        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("New password must be different from current password for user {UserId}", user.Id);
            return false;
        }


        var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.NewPassword);


        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.UpdatedAt = DateTime.UtcNow;


        await _tokenService.RevokeUserSessionsAsync(user.Id, cancellationToken);


        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully changed for user {UserId}", user.Id);


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
        }

        return true;
    }
}
