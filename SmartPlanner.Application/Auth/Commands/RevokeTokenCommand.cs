
using MediatR;

namespace SmartPlanner.Application.Auth.Commands;

public record RevokeTokenCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}
