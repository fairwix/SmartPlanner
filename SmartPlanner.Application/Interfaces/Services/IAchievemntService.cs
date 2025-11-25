using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Domain.Interfaces.Services
{
    public interface IAchievementService
    {
        Task<List<Achievement>> GetAllAchievementsAsync(CancellationToken cancellationToken = default);
        Task<List<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> AwardAchievementToUserAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default);
        Task CheckAndAwardAchievementsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Achievement>> GetEligibleAchievementsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Achievement>> GetAchievementsByTypeAsync(AchievementType type, CancellationToken cancellationToken = default);
        Task<Achievement?> GetAchievementByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}