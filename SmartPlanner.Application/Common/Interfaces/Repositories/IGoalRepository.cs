using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Common.Interfaces.Repositories;

    public class GoalStats
    {
        public int TotalGoals { get; set; }
        public int CompletedGoals { get; set; }
        public int ActiveGoals { get; set; }
        public int OverdueGoals { get; set; }
        public double CompletionRate { get; set; }
        public double AverageProgress { get; set; }
        public int GoalsDueThisWeek { get; set; }
        public int HighPriorityGoals { get; set; }
    }

    public interface IGoalRepository
    {
        // Базовые операции
        Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Goal> CreateAsync(Goal entity, CancellationToken cancellationToken = default);
        Task<Goal?> UpdateAsync(Goal entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        // Специфичные методы
        Task<PagedResult<Goal>> GetUserGoalsWithPaginationAsync(
            Guid userId,
            PaginationRequest pagination,
            string? category = null,
            string? priority = null,
            bool? completed = null,
            string? searchTerm = null,
            CancellationToken cancellationToken = default);

        Task<List<Goal>> GetActiveGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Goal>> GetCompletedGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Goal>> GetOverdueGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<GoalStats> GetUserGoalStatsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsGoalTitleUniqueForUserAsync(Guid userId, string title, CancellationToken cancellationToken = default);
        Task<PagedResult<Goal>> GetUserGoalsWithAdvancedFilteringAsync(
            Guid userId,
            AdvancedPaginationRequest pagination,
            CancellationToken cancellationToken = default);

        Task<List<Goal>> GetUserGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
