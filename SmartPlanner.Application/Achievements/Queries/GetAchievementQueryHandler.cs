using MediatR;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Application.Achievements.Queries
{
    public class GetAchievementsQueryHandler : IRequestHandler<GetAchievementsQuery, List<AchievementDto>>
    {
        private readonly IAchievementRepository _achievementRepository;

        public GetAchievementsQueryHandler(IAchievementRepository achievementRepository)
        {
            _achievementRepository = achievementRepository;
        }

        public async Task<List<AchievementDto>> Handle(GetAchievementsQuery request, CancellationToken cancellationToken)
        {
            var achievements = await _achievementRepository.GetAllAsync(cancellationToken);
            
            // Фильтрация по типу, если указана
            if (!string.IsNullOrEmpty(request.AchievementType))
            {
                // Здесь должна быть логика фильтрации по типу
                // achievements = achievements.Where(a => a.Type == request.AchievementType).ToList();
            }

            // Маппинг в DTO
            return achievements.Select(a => new AchievementDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                // Добавьте остальные поля
            }).ToList();
        }
    }
}