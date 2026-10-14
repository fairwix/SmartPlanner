using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Commands;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found or already deleted", request.UserId);
            return false;
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        // RevokeUserSessionsAsync saves tracked changes when active sessions exist.
        await _tokenService.RevokeUserSessionsAsync(user.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(
            SecurityEventType.UserDeleted,
            user.Id,
            user.Email,
            success: true,
            details: new { DeletedBy = request.DeletedBy },
            cancellationToken: cancellationToken);

        _logger.LogInformation("User {UserId} soft-deleted by admin {AdminId}", user.Id, request.DeletedBy);
        return true;
    }
}
