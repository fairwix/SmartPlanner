using MediatR;

namespace SmartPlanner.Application.Users.Commands;

public record BlockUserCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
    public Guid BlockedBy { get; init; } // Админ, который блокирует
}
