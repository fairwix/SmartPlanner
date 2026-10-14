using MediatR;

namespace SmartPlanner.Application.Users.Commands;

public record UnblockUserCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
    public Guid UnblockedBy { get; init; }
}
