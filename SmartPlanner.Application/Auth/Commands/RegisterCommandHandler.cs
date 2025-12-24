using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Models;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Commands
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<RegisterCommandHandler> _logger;
        private readonly IConfirmationTokenService _confirmationTokenService;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly AppSettings _appSettings;

        public RegisterCommandHandler(
            IApplicationDbContext context,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            ILogger<RegisterCommandHandler> logger,
            IConfirmationTokenService confirmationTokenService,
            IEmailService emailService,
            IAuditService auditService,
            IOptions<AppSettings> appSettings)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _confirmationTokenService = confirmationTokenService;
            _emailService = emailService;
            _auditService = auditService;
            _appSettings = appSettings.Value;
        }

        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering new user: {Email}", request.Email);

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                throw new ArgumentException($"Email {request.Email} is already registered");
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username, cancellationToken);

            if (usernameExists)
            {
                throw new ArgumentException($"Username {request.Username} is already taken");
            }

            var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = request.PhoneNumber,
                IsEmailConfirmed = false,
                IsActive = true,
                LastLoginAt = DateTime.UtcNow
            };


            // Временно создаем роль если ее нет
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole == null)
            {
                userRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    NormalizedName = "USER",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Roles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            if (userRole != null)
            {
                var userRoleEntity = new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = null
                };

                _context.UserRoles.Add(userRoleEntity);
            }

            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var confirmationToken = await _confirmationTokenService.GenerateEmailConfirmationTokenAsync(
                user.Id, cancellationToken);

            var confirmationLink = $"{_appSettings.BaseUrl}/api/auth/confirm-email?" +
                                  $"userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user, cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailConfirmationAsync(
                        user.Email,
                        user.Username,
                        confirmationLink);

                    _logger.LogInformation("Confirmation email sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                }
            }, cancellationToken);

            var accesToken = await _tokenService.GenerateAccessTokenAsync(user, cancellationToken);
            var (refreshToken, refreshTokenHash) = _tokenService.GenerateRefreshToken();


            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_appSettings.Jwt.RefreshTokenExpirationDays);

            await _tokenService.CreateUserSessionAsync(
                user.Id,
                refreshTokenHash,
                refreshTokenExpiry,
                null,
                null,
                cancellationToken);

            await _auditService.LogSecurityEventAsync(
                SecurityEventType.Register,
                user.Id,
                user.Email,
                success: true,
                details: new {
                    Username = user.Username,
                    HasConfirmedEmail = false,
                    Roles = new[] { "User" }
                },
                cancellationToken: cancellationToken);

            var response = new AuthResponseDto(
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddMinutes(_appSettings.Jwt.AccessTokenExpirationMinutes),
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
                    new List<string> { "User" },
                    new List<string>()));

            _logger.LogInformation("User {Email} registered successfully (email confirmation pending)", request.Email);

            return response;
        }
    }
}
