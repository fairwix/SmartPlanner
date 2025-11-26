using MediatR;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Application.Challenges.Queries;

    public record GetUserChallengesQuery : IRequest<List<ChallengeDto>>
    {
        public Guid UserId { get; init; }
        public bool IncludeCompleted { get; init; } = false;
    }
