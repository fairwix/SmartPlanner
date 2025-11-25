using MediatR;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Application.Challenges.Queries
{
    public record GetChallengeByIdQuery : IRequest<ChallengeDto?>
    {
        public Guid ChallengeId { get; init; }
    }
}