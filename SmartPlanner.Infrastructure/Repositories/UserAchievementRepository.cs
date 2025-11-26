using Microsoft.Extensions.Options;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Configuration;

namespace SmartPlanner.Infrastructure.Repositories;

    public class UserAchievementRepository : FileStorageRepository<UserAchievement>, IUserAchievementRepository
    {
        public UserAchievementRepository(IOptions<FileStorageOptions> options) : base(options.Value.UserAchievementsFilePath) { }

        // ✅ ДОБАВЛЯЕМ базовые методы из интерфейса
        public async Task<UserAchievement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.GetByIdAsync(id, cancellationToken);

        public async Task<UserAchievement> CreateAsync(UserAchievement entity, CancellationToken cancellationToken = default)
            => await base.CreateAsync(entity, cancellationToken);

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.DeleteAsync(id, cancellationToken);

        public async Task<List<UserAchievement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userAchievements = await base.GetAllAsync(cancellationToken);
            return userAchievements.Where(ua => ua.UserId == userId).ToList();
        }

        public async Task<List<UserAchievement>> GetByAchievementIdAsync(Guid achievementId, CancellationToken cancellationToken = default)
        {
            var userAchievements = await base.GetAllAsync(cancellationToken);
            return userAchievements.Where(ua => ua.AchievementId == achievementId).ToList();
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default)
        {
            var userAchievements = await base.GetAllAsync(cancellationToken);
            return userAchievements.Any(ua => ua.UserId == userId && ua.AchievementId == achievementId);
        }

        public async Task<UserAchievement?> GetByUserAndAchievementAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default)
        {
            var userAchievements = await base.GetAllAsync(cancellationToken);
            return userAchievements.FirstOrDefault(ua => ua.UserId == userId && ua.AchievementId == achievementId);
        }
    }
