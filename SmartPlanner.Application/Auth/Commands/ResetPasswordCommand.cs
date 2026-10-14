using MediatR;

namespace SmartPlanner.Application.Auth.Commands;

public record ResetPasswordCommand : IRequest<bool>
{
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}
