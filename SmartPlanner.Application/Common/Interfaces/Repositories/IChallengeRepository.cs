using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Interfaces.Repositories
{
    public interface IChallengeRepository
    {
        // Базовые операции
        Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Challenge>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Challenge> CreateAsync(Challenge entity, CancellationToken cancellationToken = default);
        Task<Challenge?> UpdateAsync(Challenge entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        
        // Специфичные методы
        Task<List<Challenge>> GetActiveChallengesAsync(CancellationToken cancellationToken = default);
        Task<List<Challenge>> GetUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Challenge>> GetExpiredChallengesAsync(CancellationToken cancellationToken = default);
        Task<bool> AddParticipantToChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> RemoveParticipantFromChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default);
        Task<List<ChallengeParticipant>> GetChallengeParticipantsAsync(Guid challengeId, CancellationToken cancellationToken = default);
        Task<bool> UpdateChallengeProgressAsync(Guid challengeId, int progress, CancellationToken cancellationToken = default);
    }
}