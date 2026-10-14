using MediatR;

namespace SmartPlanner.Application.Users.Commands;

public record DeleteUserCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public Guid DeletedBy { get; init; }
}
