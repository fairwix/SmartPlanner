using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Commands;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BlockUserCommandHandler> _logger;

    public BlockUserCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<BlockUserCommandHandler> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Unit> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blocking user {UserId} by admin {AdminId}", request.UserId, request.BlockedBy);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new ArgumentException($"User with ID {request.UserId} not found or deleted");

        if (!user.IsActive)
        {
            _logger.LogWarning("User {UserId} is already blocked", request.UserId);
            return Unit.Value;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _tokenService.RevokeUserSessionsAsync(user.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            SecurityEventType.UserBlocked,
            user.Id,
            user.Email,
            success: true,
            details: new { BlockedBy = request.BlockedBy },
            cancellationToken: cancellationToken);

        _logger.LogInformation("User {UserId} blocked and all sessions revoked", request.UserId);
        return Unit.Value;
    }
}
