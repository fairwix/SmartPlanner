// SmartPlanner.Application/AI/Queries/GeneratePersonalChallengesQuery.cs
using MediatR;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Application.AI.Queries;

    public record GeneratePersonalChallengesQuery : IRequest<List<ChallengeDto>>
    {
        public Guid UserId { get; init; }
        public int Count { get; init; } = 3;
    }
