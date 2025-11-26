using MediatR;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Application.Achievements.Queries;

    public class GetAchievementByIdQueryHandler : IRequestHandler<GetAchievementByIdQuery, AchievementDto?>
    {
        private readonly IAchievementRepository _achievementRepository;

        public GetAchievementByIdQueryHandler(IAchievementRepository achievementRepository)
        {
            _achievementRepository = achievementRepository;
        }

        public async Task<AchievementDto?> Handle(GetAchievementByIdQuery request, CancellationToken cancellationToken)
        {
            var achievement = await _achievementRepository.GetByIdAsync(request.AchievementId, cancellationToken);

            if (achievement == null)
                return null;

            return new AchievementDto(
                achievement.Id,
                achievement.CreatedAt,
                achievement.UpdatedAt,
                achievement.Name,
                achievement.Description,
                achievement.BadgeImage,
                achievement.RewardAmount,
                achievement.Type.ToString(),
                achievement.Condition);
        }
    }
