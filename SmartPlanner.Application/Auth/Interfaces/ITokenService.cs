using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Auth.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default);
        (string RefreshToken, string RefreshTokenHash) GenerateRefreshToken();

        Task<UserSession> CreateUserSessionAsync(
            Guid userId,
            string refreshTokenHash,
            DateTime expiresAt,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId, CancellationToken cancellationToken = default);
        Task RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task RevokeSessionAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<List<UserSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

        bool ValidateAccessToken(string token, out ClaimsPrincipal? principal);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
