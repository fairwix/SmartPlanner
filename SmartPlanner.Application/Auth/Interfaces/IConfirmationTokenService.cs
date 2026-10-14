namespace SmartPlanner.Application.Auth.Interfaces;

public interface IConfirmationTokenService
{
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ValidatePasswordResetTokenAsync(string token, Guid userId, CancellationToken cancellationToken);
    Task<bool> ValidateEmailConfirmationTokenAsync(string token, Guid userId, CancellationToken cancellationToken);
    Task RevokePasswordResetTokenAsync(string token, CancellationToken cancellationToken);
    Task MarkEmailTokenAsUsedAsync(string token, CancellationToken cancellationToken);
}
