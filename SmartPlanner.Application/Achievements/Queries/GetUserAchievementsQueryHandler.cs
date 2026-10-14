using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Achievements.Queries
{
    public class GetUserAchievementsQueryHandler : IRequestHandler<GetUserAchievementsQuery, List<UserAchievementDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetUserAchievementsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserAchievementDto>> Handle(GetUserAchievementsQuery request, CancellationToken cancellationToken)
        {
            var userAchievements = await _context.UserAchievements
                .Include(ua => ua.Achievement)
                .Where(ua => ua.UserId == request.UserId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return userAchievements.Select(ua => new UserAchievementDto(
                ua.Id,
                ua.CreatedAt,
                ua.UpdatedAt ?? ua.CreatedAt,
                ua.UserId,
                ua.AchievementId,
                ua.Achievement?.Name ?? string.Empty,
                ua.Achievement?.Description ?? string.Empty,
                ua.Achievement?.BadgeImage ?? string.Empty,
                ua.AwardedAt)).ToList();
        }
    }
}
