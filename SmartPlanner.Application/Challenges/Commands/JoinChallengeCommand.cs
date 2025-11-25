using MediatR;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Application.Challenges.Commands
{
    public record JoinChallengeCommand : IRequest<ChallengeDto>
    {
        public Guid ChallengeId { get; init; }
        public Guid UserId { get; init; }
    }
}