using MediatR;
using SmartPlanner.Application.Auth.Interfaces;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Commands
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<LogoutCommandHandler> _logger;
        private readonly IAuditService _auditService;

        public LogoutCommandHandler(
            ITokenService tokenService,
            ILogger<LogoutCommandHandler> logger,
            IAuditService auditService)
        {
            _tokenService = tokenService;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Logging out user {UserId}", request.UserId);

            // Отзыв всех сессий пользователя
            await _tokenService.RevokeUserSessionsAsync(request.UserId, cancellationToken);
            // Логируем выход
            await _auditService.LogSecurityEventAsync(
                SecurityEventType.Logout,
                request.UserId,
                ipAddress: null, // Можно получить из HttpContext
                success: true,
                cancellationToken: cancellationToken);

            return Unit.Value;
        }
    }
}
