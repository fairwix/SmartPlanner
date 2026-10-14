using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Auth.Commands;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RevokeTokenCommandHandler> _logger;

    public RevokeTokenCommandHandler(
        ITokenService tokenService,
        IApplicationDbContext context,
        ILogger<RevokeTokenCommandHandler> logger)
    {
        _tokenService = tokenService;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing revoke token for user {UserId}", request.UserId);

        try
        {
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == request.UserId && u.IsActive && !u.IsDeleted,
                    cancellationToken);

            if (!userExists)
            {
                _logger.LogWarning("User {UserId} not found or inactive for token revocation",
                    request.UserId);
                return false;
            }

            var session = await _tokenService.GetSessionByRefreshTokenAsync(
                request.RefreshToken, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Session not found for refresh token");
                return false;
            }

            if (session.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to revoke token belonging to user {TokenUserId}",
                    request.UserId, session.UserId);
                return false;
            }

            await _tokenService.RevokeSessionAsync(request.RefreshToken, cancellationToken);

            _logger.LogInformation("Token revoked successfully for user {UserId}", request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token for user {UserId}", request.UserId);
            return false;
        }
    }
}
