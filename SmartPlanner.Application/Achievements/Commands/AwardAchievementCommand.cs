using MediatR;

namespace SmartPlanner.Application.Achievements.Commands;

    public record AwardAchievementCommand : IRequest<bool>
    {
        public Guid UserId { get; init; }
        public Guid AchievementId { get; init; }
    }
