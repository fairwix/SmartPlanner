using MediatR;

namespace SmartPlanner.Application.Auth.Commands;

public record ForgotPasswordCommand : IRequest<bool>
{
    public string Email { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
