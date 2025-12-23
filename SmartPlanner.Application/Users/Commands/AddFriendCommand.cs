using MediatR;

namespace SmartPlanner.Application.Users.Commands;

    public record AddFriendCommand : IRequest<bool>
    {
        public Guid UserId { get; init; }
        public Guid FriendId { get; init; }
    }
