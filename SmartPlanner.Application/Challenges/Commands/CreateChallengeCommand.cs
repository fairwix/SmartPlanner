using MediatR;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Application.Challenges.Commands
{
    public record CreateChallengeCommand : IRequest<ChallengeDto>
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public bool IsGroupChallenge { get; init; }
        public int TargetValue { get; init; }
        public Guid CreatedBy { get; init; }
    }
}