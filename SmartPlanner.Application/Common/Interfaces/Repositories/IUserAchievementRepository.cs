using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Common.Interfaces.Repositories
{
    public interface IUserAchievementRepository
    {
        // Базовые операции (минимальный необходимый набор)
        Task<UserAchievement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<UserAchievement> CreateAsync(UserAchievement entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        
        // Специфичные методы
        Task<List<UserAchievement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<UserAchievement>> GetByAchievementIdAsync(Guid achievementId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default);
        Task<UserAchievement?> GetByUserAndAchievementAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default);
    }
}