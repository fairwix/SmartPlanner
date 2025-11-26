using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.DTOs.Challenge;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Interfaces.Services;

    public interface IChallengeService
    {
        Task<Challenge> CreateChallengeAsync(CreateChallengeRequest request, CancellationToken cancellationToken = default);
        Task<Challenge?> GetChallengeByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Challenge>> GetActiveChallengesAsync(CancellationToken cancellationToken = default);
        Task<List<Challenge>> GetUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> JoinChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> LeaveChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default);
        Task<Challenge> UpdateChallengeProgressAsync(Guid challengeId, int progress, CancellationToken cancellationToken = default);
        Task<List<Challenge>> GenerateAiChallengesAsync(Guid userId, int count = 3, CancellationToken cancellationToken = default);
    }

