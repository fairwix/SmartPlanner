using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Common.Interfaces.Repositories;

    public interface IAchievementRepository
    {
        // Базовые операции (только те, которые действительно нужны)
        Task<Achievement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Achievement>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Achievement> CreateAsync(Achievement entity, CancellationToken cancellationToken = default);
        Task<Achievement?> UpdateAsync(Achievement entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        // Специфичные методы
        Task<List<Achievement>> GetAchievementsByTypeAsync(AchievementType type, CancellationToken cancellationToken = default);
    }
