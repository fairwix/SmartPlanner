using SmartPlanner.Application.AI.Queries;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Infrastructure.AI;

    public class ChallengeRecommendationService
    {
        private readonly GeneratePersonalChallengesQueryHandler _queryHandler;

        public ChallengeRecommendationService(GeneratePersonalChallengesQueryHandler queryHandler)
        {
            _queryHandler = queryHandler;
        }

        public async Task<List<ChallengeDto>> GenerateSmartChallengesAsync(
            Guid userId,
            int count = 3,
            CancellationToken cancellationToken = default) // ← ДОБАВИТЬ ЭТУ СТРОЧКУ
        {
            var query = new GeneratePersonalChallengesQuery { UserId = userId, Count = count };
            return await _queryHandler.Handle(query, cancellationToken); // ✅ Теперь токен доступен
        }
    }
