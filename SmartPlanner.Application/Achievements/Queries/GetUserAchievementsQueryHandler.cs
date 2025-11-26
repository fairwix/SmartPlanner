using MediatR;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Achievements.Queries;

    public class GetUserAchievementsQueryHandler : IRequestHandler<GetUserAchievementsQuery, List<UserAchievementDto>>
    {
        private readonly IUserAchievementRepository _userAchievementRepository;

        public GetUserAchievementsQueryHandler(IUserAchievementRepository userAchievementRepository)
        {
            _userAchievementRepository = userAchievementRepository;
        }

        public async Task<List<UserAchievementDto>> Handle(GetUserAchievementsQuery request, CancellationToken cancellationToken)
        {
            var userAchievements = await _userAchievementRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            return userAchievements.Select(ua => new UserAchievementDto(
                ua.Id,
                ua.CreatedAt,
                ua.UpdatedAt,
                ua.UserId,
                ua.AchievementId,
                ua.Achievement.Name,
                ua.Achievement.Description,
                ua.Achievement.BadgeImage,
                ua.AwardedAt)).ToList();
        }
    }
