using MediatR;

namespace SmartPlanner.Application.Auth.Commands;

public record ChangePasswordCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
