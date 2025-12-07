using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Interfaces.Services;

public interface IAchievementCheckerService
{
    Task<List<Achievement>> CheckAndAwardEligibleAchievementsAsync(
        Guid userId,
        IApplicationDbContext context,
        CancellationToken cancellationToken);

    bool MeetsAchievementCondition(Achievement achievement, User user, int completedGoalsCount, int friendsCount);
}
