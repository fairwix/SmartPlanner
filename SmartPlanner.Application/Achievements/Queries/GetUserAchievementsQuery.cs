// SmartPlanner.Application/Achievements/Queries/GetUserAchievementsQuery.cs
using MediatR;
using SmartPlanner.Application.Achievements.Dtos;

namespace SmartPlanner.Application.Achievements.Queries
{
    public record GetUserAchievementsQuery : IRequest<List<UserAchievementDto>>
    {
        public Guid UserId { get; init; }
    }
}