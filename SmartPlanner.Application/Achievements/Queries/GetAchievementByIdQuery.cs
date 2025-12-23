using MediatR;
using SmartPlanner.Application.Achievements.Dtos;

namespace SmartPlanner.Application.Achievements.Queries;

    public record GetAchievementByIdQuery : IRequest<AchievementDto?>
    {
        public Guid AchievementId { get; init; }
    }
