using MediatR;
using SmartPlanner.Application.Achievements.Dtos;

namespace SmartPlanner.Application.Achievements.Queries;

    public record GetAchievementsQuery : IRequest<List<AchievementDto>>
    {
        public string? AchievementType { get; init; }
    }
