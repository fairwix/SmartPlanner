using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly IAuditService _auditService;

        public LoginCommandHandler(
            IApplicationDbContext context,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            ILogger<LoginCommandHandler> logger,
            IAuditService auditService)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Login attempt for: {Identifier}", request.EmailOrUsername);

            // 1. Поиск пользователя по email или username
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u =>
                    u.Email == request.EmailOrUsername ||
                    u.Username == request.EmailOrUsername,
                    cancellationToken);

            if (user == null)
            {
                await _auditService.LogSecurityEventAsync(
                    SecurityEventType.FailedLogin,
                    email: request.EmailOrUsername,
                    ipAddress: request.IpAddress,
                    userAgent: request.DeviceInfo,
                    success: false,
                    details: new { Reason = "User not found" },
                    cancellationToken: cancellationToken);

                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // 2. Проверка активности пользователя (ТЗ)
            if (!user.IsActive)
            {
                // Логируем с причиной
                await _auditService.LogSecurityEventAsync(
                    SecurityEventType.FailedLogin,
                    user.Id,
                    user.Email,
                    request.IpAddress,
                    request.DeviceInfo,
                    success: false,
                    details: new {
                        Reason = "Account issue",
                        IsActive = user.IsActive,
                        IsDeleted = user.IsDeleted,
                        IsEmailConfirmed = user.IsEmailConfirmed
                    },
                    cancellationToken: cancellationToken);

                throw new UnauthorizedAccessException("Account issue");
            }

            if (user.IsDeleted)
            {
                _logger.LogWarning("Deleted user attempted login: {UserId}", user.Id);
                throw new UnauthorizedAccessException("Account not found");
            }

            // 3. Проверка пароля
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                // Логируем неверный пароль
                await _auditService.LogSecurityEventAsync(
                    SecurityEventType.FailedLogin,
                    user.Id,
                    user.Email,
                    request.IpAddress,
                    request.DeviceInfo,
                    success: false,
                    details: new { Reason = "Invalid password" },
                    cancellationToken: cancellationToken);

                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // 4. Проверка подтверждения email (опционально по ТЗ)
            if (!user.IsEmailConfirmed)
            {
                _logger.LogWarning("User with unconfirmed email attempted login: {UserId}", user.Id);
                throw new UnauthorizedAccessException("Email not confirmed. Please confirm your email before logging in.");
            }

            // Логируем успешный вход
            await _auditService.LogSecurityEventAsync(
                SecurityEventType.Login,
                user.Id,
                user.Email,
                request.IpAddress,
                request.DeviceInfo,
                success: true,
                cancellationToken: cancellationToken);

            // 5. Обновление LastLoginAt
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // 6. Генерация токенов
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user, cancellationToken);
            var (refreshToken, refreshTokenHash) = _tokenService.GenerateRefreshToken();

            // 7. Создание сессии
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _tokenService.CreateUserSessionAsync(
                user.Id,
                refreshTokenHash,
                refreshTokenExpiry,
                request.DeviceInfo,
                request.IpAddress,
                cancellationToken);

            // 8. Получение ролей и permissions
            var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
                .Distinct()
                .ToList();

            // 9. Формирование ответа
            var response = new AuthResponseDto(
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddMinutes(30), // Из конфигурации
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

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return response;
        }
    }
}
