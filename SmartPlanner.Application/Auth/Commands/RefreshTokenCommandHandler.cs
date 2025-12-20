    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using SmartPlanner.Application.Auth.Dtos;
    using SmartPlanner.Application.Auth.Interfaces;
    using SmartPlanner.Application.Common.Interfaces;

    namespace SmartPlanner.Application.Auth.Commands
    {
        public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
        {
            private readonly IApplicationDbContext _context;
            private readonly ITokenService _tokenService;
            private readonly ILogger<RefreshTokenCommandHandler> _logger;

            public RefreshTokenCommandHandler(
                IApplicationDbContext context,
                ITokenService tokenService,
                ILogger<RefreshTokenCommandHandler> logger)
            {
                _context = context;
                _tokenService = tokenService;
                _logger = logger;
            }

            public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
            {
                _logger.LogDebug("Processing refresh token request");

                // Получить старую сессию перед её отзывом
                var oldSession = await _tokenService.GetSessionByRefreshTokenAsync(
                    request.RefreshToken, cancellationToken);

                if (oldSession == null)
                {
                    _logger.LogWarning("Old session not found during refresh");
                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                // Извлечение principal из истёкшего токена
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal == null)
                {
                    _logger.LogWarning("Invalid access token in refresh request");
                    throw new UnauthorizedAccessException("Invalid token");
                }

                // Получение UserId из claims
                var userIdClaim = principal.FindFirst("userId")?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Invalid userId in token");
                    throw new UnauthorizedAccessException("Invalid token");
                }

                // Проверка refresh token
                var isValid = await _tokenService.ValidateRefreshTokenAsync(
                    request.RefreshToken,
                    userId,
                    cancellationToken);

                if (!isValid)
                {
                    _logger.LogWarning("Invalid refresh token for user {UserId}", userId);
                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                // Находим пользователя
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found or inactive", userId);
                    throw new UnauthorizedAccessException("User not found");
                }

                // Ротация: отзываем старый refresh token
                await _tokenService.RevokeSessionAsync(request.RefreshToken, cancellationToken);
                _logger.LogDebug("Old session revoked for user {UserId}", userId);

                // Генерируем новую пару токенов
                var accessToken = await _tokenService.GenerateAccessTokenAsync(user, cancellationToken);
                var (newRefreshToken, newRefreshTokenHash) = _tokenService.GenerateRefreshToken();

                // Создаем новую сессию с ТЕМИ ЖЕ метаданными
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await _tokenService.CreateUserSessionAsync(
                    user.Id,
                    newRefreshTokenHash,
                    refreshTokenExpiry,
                    oldSession.DeviceInfo, // ← Сохраняем DeviceInfo из старой сессии
                    oldSession.IpAddress,  // ← Сохраняем IpAddress из старой сессии
                    cancellationToken);

                _logger.LogInformation("Tokens refreshed for user {UserId}", userId);

                // Формирование ответа
                var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
                var permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
                    .Distinct()
                    .ToList();

                return new AuthResponseDto(
                    accessToken,
                    newRefreshToken,
                    DateTime.UtcNow.AddMinutes(15),
                    refreshTokenExpiry,
                    new UserProfileDto(
                        user.Id,
                        user.Email,
                        user.Username,
                        user.FirstName,
                        user.LastName,
                        user.DateOfBirth,
                        user.PhoneNumber,
                        user.CreatedAt,
                        user.LastLoginAt,
                        roles,
                        permissions));
            }
        }
    }
