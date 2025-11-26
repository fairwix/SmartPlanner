using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SmartPlanner.Application.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Configuration;

namespace SmartPlanner.Infrastructure.Repositories;

    public class ChallengeRepository : FileStorageRepository<Challenge>, IChallengeRepository
    {
        public ChallengeRepository(IOptions<FileStorageOptions> options) : base(options.Value.ChallengesFilePath) { }

        // Базовые методы из интерфейса
        public async Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.GetByIdAsync(id, cancellationToken);

        public async Task<List<Challenge>> GetAllAsync(CancellationToken cancellationToken = default)
            => await base.GetAllAsync(cancellationToken);

        public async Task<Challenge> CreateAsync(Challenge entity, CancellationToken cancellationToken = default)
            => await base.CreateAsync(entity, cancellationToken);

        public async Task<Challenge?> UpdateAsync(Challenge entity, CancellationToken cancellationToken = default)
            => await base.UpdateAsync(entity, cancellationToken);

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.DeleteAsync(id, cancellationToken);

        // Специфичные методы (без изменений)
        public async Task<List<Challenge>> GetActiveChallengesAsync(CancellationToken cancellationToken = default)
        {
            var challenges = await base.GetAllAsync(cancellationToken);
            return challenges.Where(c => c.IsActive).ToList();
        }

        public async Task<List<Challenge>> GetUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var challenges = await base.GetAllAsync(cancellationToken);
            return challenges.Where(c =>
                c.CreatedBy == userId ||
                c.Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Joined)
            ).ToList();
        }

        public async Task<List<Challenge>> GetExpiredChallengesAsync(CancellationToken cancellationToken = default)
        {
            var challenges = await base.GetAllAsync(cancellationToken);
            return challenges.Where(c => c.IsExpired()).ToList();
        }

        public async Task<bool> AddParticipantToChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default)
        {
            var challenge = await GetByIdAsync(challengeId, cancellationToken);

            if (challenge == null || !challenge.CanUserJoin(userId))
                return false;
            challenge.Participants ??= new List<ChallengeParticipant>();
            if (challenge.Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Joined))
                return false;

            var participants = challenge.Participants.ToList();
            participants.Add(new ChallengeParticipant
            {
                Id = Guid.NewGuid(),
                ChallengeId = challengeId,
                UserId = userId,
                Status = ParticipantStatus.Joined,
                JoinedAt = DateTime.UtcNow
            });
            challenge.Participants = participants;

            await UpdateAsync(challenge, cancellationToken);
            return true;
        }

        public async Task<bool> RemoveParticipantFromChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default)
        {
            var challenge = await GetByIdAsync(challengeId, cancellationToken);

            if (challenge == null) return false;

            var participant = challenge.Participants.FirstOrDefault(p =>
                p.UserId == userId && p.Status == ParticipantStatus.Joined);

            if (participant == null) return false;

            var participants = challenge.Participants.ToList();
            participants.Remove(participant);
            challenge.Participants = participants;
            await UpdateAsync(challenge, cancellationToken);
            return true;
        }

        public async Task<List<ChallengeParticipant>> GetChallengeParticipantsAsync(Guid challengeId, CancellationToken cancellationToken = default)
        {
            var challenge = await GetByIdAsync(challengeId, cancellationToken);
            return challenge?.Participants.ToList() ?? new List<ChallengeParticipant>();
        }

        public async Task<bool> UpdateChallengeProgressAsync(Guid challengeId, int progress, CancellationToken cancellationToken = default)
        {
            var challenge = await GetByIdAsync(challengeId, cancellationToken);

            if (challenge == null) return false;

            challenge.CurrentValue = Math.Min(progress, challenge.TargetValue);
            await UpdateAsync(challenge, cancellationToken);
            return true;
        }
    }
