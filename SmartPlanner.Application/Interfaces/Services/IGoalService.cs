using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

using SmartPlanner.Application.DTOs.Goal;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Interfaces.Services;

    public interface IGoalService
    {
        Task<Goal> CreateGoalAsync(CreateGoalRequest request, CancellationToken cancellationToken = default);
        Task<Goal> UpdateGoalProgressAsync(Guid goalId, int progressValue, CancellationToken cancellationToken = default);
        Task<Goal> UpdateGoalAsync(Goal goal, CancellationToken cancellationToken = default);
        Task<bool> DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default);
        Task<Goal> GetGoalByIdAsync(Guid goalId, CancellationToken cancellationToken = default);
        Task<List<Goal>> GetUserGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Goal>> GetUserGoalsPagedAsync(Guid userId, int pageNumber, int pageSize, string sortBy = "CreatedAt", string sortOrder = "desc", CancellationToken cancellationToken = default);
    }

