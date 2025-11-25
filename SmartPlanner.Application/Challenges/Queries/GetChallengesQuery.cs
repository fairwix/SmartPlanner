using MediatR;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Application.Challenges.Queries
{
    public record GetChallengesQuery : IRequest<List<ChallengeDto>>
    {
        public Guid? UserId { get; init; }
        public bool ActiveOnly { get; init; } = false;
        public string? Type { get; init; }
        public bool? IsGroupChallenge { get; init; }
    }
}