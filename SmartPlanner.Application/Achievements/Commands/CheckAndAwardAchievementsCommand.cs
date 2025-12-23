using MediatR;

namespace SmartPlanner.Application.Achievements.Commands;

    public record CheckAndAwardAchievementsCommand : IRequest
    {
        public Guid UserId { get; init; }
    }
