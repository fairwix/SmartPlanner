using MediatR;

namespace SmartPlanner.Application.Challenges.Commands;

    public record LeaveChallengeCommand : IRequest<bool>
    {
        public Guid ChallengeId { get; init; }
        public Guid UserId { get; init; }
    }
