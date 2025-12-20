using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TokenService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly SigningCredentials _signingCredentials;
        private readonly IAuditService _auditService;

        // Константы для названий claims
        private const string UserIdClaim = "userId";
        private const string EmailClaim = "email";
        private const string UsernameClaim = "username";
        private const string EmailConfirmedClaim = "emailConfirmed";
        private const string IsActiveClaim = "isActive";
        private const string PermissionClaim = "permission";
        private const string CustomClaimPrefix = "custom:";

        public TokenService(
            IConfiguration configuration,
            IApplicationDbContext context,
            ILogger<TokenService> logger,
            IAuditService auditService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditService = auditService;

            _tokenHandler = new JwtSecurityTokenHandler();

            var secretKey = _configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("JWT Secret is not configured");

            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        }

        public async Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                _logger.LogDebug("Generating access token for user {UserId}", user.Id);

                // Загружаем полную информацию о пользователе с ролями и permissions
                var userWithClaims = await GetUserWithClaimsAsync(user.Id, cancellationToken);

                // Создаем claims
                var claims = BuildClaimsIdentity(userWithClaims);

                // Создаем token descriptor
                var tokenDescriptor = CreateTokenDescriptor(claims);

                // Генерируем токен
                var token = _tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = _tokenHandler.WriteToken(token);

                _logger.LogDebug("Access token generated successfully for user {UserId}", user.Id);

                await _auditService.LogSecurityEventAsync(
                    SecurityEventType.TokenRefresh,
                    user.Id,
                    user.Email,
                    ipAddress: null,
                    success: true,
                    cancellationToken: cancellationToken);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for user {UserId}", user.Id);
                throw;
            }
        }

        public (string RefreshToken, string RefreshTokenHash) GenerateRefreshToken()
        {
            try
            {
                // Генерация криптографически стойкого токена
                var randomBytes = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomBytes);

                var refreshToken = Convert.ToBase64String(randomBytes);
                var refreshTokenHash = ComputeSha256Hash(refreshToken);

                _logger.LogDebug("Refresh token generated");
                return (refreshToken, refreshTokenHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw;
            }
        }

        public async Task<UserSession> CreateUserSessionAsync(
            Guid userId,
            string refreshTokenHash,
            DateTime expiresAt,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверяем существование пользователя
                var userExists = await _context.Users
                    .AnyAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, cancellationToken);

                if (!userExists)
                    throw new ArgumentException($"User {userId} not found or inactive");

                // Создаем сессию
                var session = new UserSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RefreshTokenHash = refreshTokenHash,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    DeviceInfo = deviceInfo,
                    IpAddress = ipAddress,
                    IsRevoked = false
                };

                // Сохраняем в БД
                await _context.UserSessions.AddAsync(session, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User session created for user {UserId}", userId);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user session for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var refreshTokenHash = ComputeSha256Hash(refreshToken);

                var session = await _context.UserSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.UserId == userId &&
                        s.RefreshTokenHash == refreshTokenHash &&
                        !s.IsRevoked &&
                        s.ExpiresAt > DateTime.UtcNow,
                        cancellationToken);

                var isValid = session != null;

                if (!isValid)
                    _logger.LogWarning("Invalid refresh token for user {UserId}", userId);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
                return false;
            }
        }

        public async Task RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var sessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && !s.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var session in sessions)
                {
                    session.IsRevoked = true;
                    session.RevokedAt = DateTime.UtcNow;
                }

                if (sessions.Any())
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Revoked {Count} sessions for user {UserId}", sessions.Count, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking sessions for user {UserId}", userId);
                throw;
            }
        }

        public async Task RevokeSessionAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var refreshTokenHash = ComputeSha256Hash(refreshToken);

                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, cancellationToken);

                if (session != null)
                {
                    session.IsRevoked = true;
                    session.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Session revoked for user {UserId}", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking session by refresh token");
                throw;
            }
        }

        public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var refreshTokenHash = ComputeSha256Hash(refreshToken);

                return await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session by refresh token");
                return null;
            }
        }

        public async Task<List<UserSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.UserSessions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sessions for user {UserId}", userId);
                throw;
            }
        }

        public bool ValidateAccessToken(string token, out ClaimsPrincipal? principal)
        {
            principal = null;

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey
                };

                principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogDebug("Access token expired");
                return false;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("Invalid token signature");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation error");
                return false;
            }
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = false, // Игнорируем expiry для refresh
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey
                };

                return _tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting principal from expired token");
                return null;
            }
        }

        #region Private Methods

        private async Task<User?> GetUserWithClaimsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserClaims)
                .Include(u => u.UserInterests)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        private List<Claim> BuildClaimsIdentity(User? user)
        {
            if (user == null)
                throw new ArgumentException("User not found");

            var claims = new List<Claim>
            {
                // Standard JWT claims
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.UniqueName, user.Username),

                // Custom claims для быстрого доступа
                new(UserIdClaim, user.Id.ToString()),
                new(EmailClaim, user.Email),
                new(UsernameClaim, user.Username),
                new(EmailConfirmedClaim, user.IsEmailConfirmed.ToString(), ClaimValueTypes.Boolean),
                new(IsActiveClaim, user.IsActive.ToString(), ClaimValueTypes.Boolean),

                // Optional claims
                new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                new(ClaimTypes.Surname, user.LastName ?? string.Empty)
            };
            if (user.DateOfBirth.HasValue)
            {
                claims.Add(new Claim("dateOfBirth", user.DateOfBirth.Value.ToString("yyyy-MM-dd")));
            }
            // Добавляем роли
            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                    claims.Add(new Claim("role", userRole.Role.Name));
                }
            }

            // Добавляем permissions
            if (user.UserRoles != null)
            {
                var permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
                    .Distinct();

                foreach (var permission in permissions)
                {
                    claims.Add(new Claim(PermissionClaim, permission));
                }
            }

            // Добавляем custom claims из UserClaims
            if (user.UserClaims != null)
            {
                foreach (var userClaim in user.UserClaims)
                {
                    claims.Add(new Claim($"{CustomClaimPrefix}{userClaim.ClaimType}", userClaim.ClaimValue));
                }
            }

            return claims;
        }

        private SecurityTokenDescriptor CreateTokenDescriptor(List<Claim> claims)
        {
            // Получаем время жизни токена из конфигурации
            var accessTokenExpirationMinutes = 15; // значение по умолчанию

            var accessTokenExpirationMinutesConfig = _configuration["JwtSettings:AccessTokenExpirationMinutes"];
            if (!string.IsNullOrEmpty(accessTokenExpirationMinutesConfig))
            {
                if (int.TryParse(accessTokenExpirationMinutesConfig, out int parsedValue))
                {
                    accessTokenExpirationMinutes = parsedValue;
                }
            }

            return new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = _signingCredentials,
                NotBefore = DateTime.UtcNow.AddSeconds(-5)
            };
        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        #endregion
    }
}
