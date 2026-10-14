using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Achievements.Queries
{
    public class GetAchievementsQueryHandler : IRequestHandler<GetAchievementsQuery, List<AchievementDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetAchievementsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AchievementDto>> Handle(GetAchievementsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Achievements.AsQueryable();

            if (!string.IsNullOrEmpty(request.AchievementType))
            {
                if (Enum.TryParse<AchievementType>(request.AchievementType, true, out var type))
                {
                    query = query.Where(a => a.Type == type);
                }
            }

            var achievements = await query.ToListAsync(cancellationToken);

            return achievements.Select(a => new AchievementDto(
                a.Id,
                a.CreatedAt,
                a.UpdatedAt,
                a.Name,
                a.Description,
                a.BadgeImage,
                a.RewardAmount,
                a.Type.ToString(),
                a.Condition)).ToList();
        }
    }
}
