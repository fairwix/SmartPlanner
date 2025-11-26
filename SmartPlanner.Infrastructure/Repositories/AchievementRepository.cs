using Microsoft.Extensions.Options;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Configuration;

namespace SmartPlanner.Infrastructure.Repositories;

    public class AchievementRepository : FileStorageRepository<Achievement>, IAchievementRepository
    {
        public AchievementRepository(IOptions<FileStorageOptions> options) : base(options.Value.AchievementsFilePath) { }

        public async Task<Achievement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.GetByIdAsync(id, cancellationToken);

        public async Task<List<Achievement>> GetAllAsync(CancellationToken cancellationToken = default)
            => await base.GetAllAsync(cancellationToken);

        public async Task<Achievement> CreateAsync(Achievement entity, CancellationToken cancellationToken = default)
            => await base.CreateAsync(entity, cancellationToken);

        public async Task<Achievement?> UpdateAsync(Achievement entity, CancellationToken cancellationToken = default)
            => await base.UpdateAsync(entity, cancellationToken);

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.DeleteAsync(id, cancellationToken);

        // ✅ Специфичные методы остаются без изменений
        public async Task<List<Achievement>> GetAchievementsByTypeAsync(AchievementType type, CancellationToken cancellationToken = default)
        {
            var achievements = await base.GetAllAsync(cancellationToken);
            return achievements.Where(a => a.Type == type).ToList();
        }

        public async Task<List<Achievement>> GetEligibleAchievementsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var achievements = await base.GetAllAsync(cancellationToken);
            return achievements;
        }
    }
