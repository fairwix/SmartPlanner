using MediatR;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Application.Challenges.Commands
{
    public record UpdateChallengeProgressCommand : IRequest<ChallengeDto>
    {
        public Guid ChallengeId { get; init; }
        public int Progress { get; init; }
        public Guid UserId { get; init; } // Кто обновляет прогресс
    }
}