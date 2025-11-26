using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Infrastructure.Repositories;

    public class GoalRepository : FileStorageRepository<Goal>, IGoalRepository
    {
        public GoalRepository(string filePath) : base(filePath) { }

        // Базовые методы из интерфейса
        public async Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.GetByIdAsync(id, cancellationToken);

        public async Task<Goal> CreateAsync(Goal entity, CancellationToken cancellationToken = default)
            => await base.CreateAsync(entity, cancellationToken);

        public async Task<Goal?> UpdateAsync(Goal entity, CancellationToken cancellationToken = default)
            => await base.UpdateAsync(entity, cancellationToken);

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.DeleteAsync(id, cancellationToken);

        // Специфичные методы (без изменений)
        public async Task<List<Goal>> GetUserGoalsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return allGoals.Where(g => g.UserId == userId).ToList();
        }

        public async Task<PagedResult<Goal>> GetUserGoalsWithPaginationAsync(
            Guid userId,
            PaginationRequest pagination,
            string? category = null,
            string? priority = null,
            bool? completed = null,
            string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            var userGoals = allGoals.Where(g => g.UserId == userId).AsEnumerable();

            if (!string.IsNullOrEmpty(category))
            {
                userGoals = userGoals.Where(g =>
                    g.Category.ToString().Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(priority))
            {
                userGoals = userGoals.Where(g =>
                    g.Priority.ToString().Equals(priority, StringComparison.OrdinalIgnoreCase));
            }

            if (completed.HasValue)
            {
                userGoals = userGoals.Where(g => g.IsCompleted == completed.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                userGoals = userGoals.Where(g =>
                    g.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    g.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            userGoals = userGoals.OrderByDescending(g => g.CreatedAt);

            var userGoalsList = userGoals.ToList();
            var totalCount = userGoalsList.Count;

            var pagedGoals = userGoalsList
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToList();

            return PagedResult<Goal>.Create(pagedGoals, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<Goal>> GetActiveGoalsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return allGoals
                .Where(g => g.UserId == userId && !g.IsCompleted && !g.IsExpired())
                .ToList();
        }

        public async Task<List<Goal>> GetCompletedGoalsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return allGoals
                .Where(g => g.UserId == userId && g.IsCompleted)
                .ToList();
        }

        public async Task<List<Goal>> GetOverdueGoalsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return allGoals
                .Where(g => g.UserId == userId && g.IsExpired() && !g.IsCompleted)
                .ToList();
        }

        public async Task<GoalStats> GetUserGoalStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            var userGoals = allGoals.Where(g => g.UserId == userId).ToList();

            var totalGoals = userGoals.Count;
            var completedGoals = userGoals.Count(g => g.IsCompleted);
            var activeGoals = userGoals.Count(g => !g.IsCompleted && !g.IsExpired());
            var overdueGoals = userGoals.Count(g => g.IsExpired() && !g.IsCompleted);
            var completionRate = totalGoals > 0 ? (double)completedGoals / totalGoals * 100 : 0;

            return new GoalStats
            {
                TotalGoals = totalGoals,
                CompletedGoals = completedGoals,
                ActiveGoals = activeGoals,
                OverdueGoals = overdueGoals,
                CompletionRate = Math.Round(completionRate, 2)
            };
        }

        public async Task<bool> IsGoalTitleUniqueForUserAsync(Guid userId, string title, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return !allGoals.Any(g =>
                g.UserId == userId &&
                g.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<PagedResult<Goal>> GetUserGoalsWithAdvancedFilteringAsync(
            Guid userId,
            AdvancedPaginationRequest pagination,
            CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            var userGoals = allGoals.Where(g => g.UserId == userId).AsEnumerable();

            if (!string.IsNullOrEmpty(pagination.Search))
            {
                userGoals = userGoals.Where(g =>
                    g.Title.Contains(pagination.Search, StringComparison.OrdinalIgnoreCase) ||
                    g.Description.Contains(pagination.Search, StringComparison.OrdinalIgnoreCase));
            }

            if (pagination.CreatedFrom.HasValue)
            {
                userGoals = userGoals.Where(g => g.CreatedAt >= pagination.CreatedFrom.Value);
            }

            if (pagination.CreatedTo.HasValue)
            {
                userGoals = userGoals.Where(g => g.CreatedAt <= pagination.CreatedTo.Value);
            }

            if (pagination.DueDateFrom.HasValue)
            {
                userGoals = userGoals.Where(g => g.DueDate >= pagination.DueDateFrom.Value);
            }

            if (pagination.DueDateTo.HasValue)
            {
                userGoals = userGoals.Where(g => g.DueDate <= pagination.DueDateTo.Value);
            }

            if (pagination.Categories?.Any() == true)
            {
                var categories = pagination.Categories
                    .Select(c => Enum.Parse<GoalCategory>(c))
                    .ToArray();
                userGoals = userGoals.Where(g => categories.Contains(g.Category));
            }

            if (pagination.Priorities?.Any() == true)
            {
                var priorities = pagination.Priorities
                    .Select(p => Enum.Parse<GoalPriority>(p))
                    .ToArray();
                userGoals = userGoals.Where(g => priorities.Contains(g.Priority));
            }

            if (pagination.IsCompleted.HasValue)
            {
                userGoals = userGoals.Where(g => g.IsCompleted == pagination.IsCompleted.Value);
            }

            if (pagination.IsExpired.HasValue)
            {
                userGoals = userGoals.Where(g => g.IsExpired() == pagination.IsExpired.Value);
            }

            if (pagination.IsOnTrack.HasValue)
            {
                userGoals = userGoals.Where(g => g.IsOnTrack() == pagination.IsOnTrack.Value);
            }

            if (pagination.MinProgress.HasValue)
            {
                userGoals = userGoals.Where(g => g.ProgressPercentage >= pagination.MinProgress.Value);
            }

            if (pagination.MaxProgress.HasValue)
            {
                userGoals = userGoals.Where(g => g.ProgressPercentage <= pagination.MaxProgress.Value);
            }

            var sortedGoals = pagination.SortBy?.ToLower() switch
            {
                "title" => pagination.SortOrder == "desc"
                    ? userGoals.OrderByDescending(g => g.Title)
                    : userGoals.OrderBy(g => g.Title),
                "duedate" => pagination.SortOrder == "desc"
                    ? userGoals.OrderByDescending(g => g.DueDate)
                    : userGoals.OrderBy(g => g.DueDate),
                "priority" => pagination.SortOrder == "desc"
                    ? userGoals.OrderByDescending(g => g.Priority)
                    : userGoals.OrderBy(g => g.Priority),
                "progress" => pagination.SortOrder == "desc"
                    ? userGoals.OrderByDescending(g => g.ProgressPercentage)
                    : userGoals.OrderBy(g => g.ProgressPercentage),
                "category" => pagination.SortOrder == "desc"
                    ? userGoals.OrderByDescending(g => g.Category)
                    : userGoals.OrderBy(g => g.Category),
                _ => pagination.SortOrder == "desc"
                    ? userGoals.OrderByDescending(g => g.CreatedAt)
                    : userGoals.OrderBy(g => g.CreatedAt)
            };

            var userGoalsList = sortedGoals.ToList();
            var totalCount = userGoalsList.Count;

            var pagedGoals = userGoalsList
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToList();

            return PagedResult<Goal>.Create(pagedGoals, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<Goal>> GetGoalsByCategoryAsync(Guid userId, GoalCategory category, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return allGoals
                .Where(g => g.UserId == userId && g.Category == category)
                .ToList();
        }

        public async Task<List<Goal>> GetGoalsByPriorityAsync(Guid userId, GoalPriority priority, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return allGoals
                .Where(g => g.UserId == userId && g.Priority == priority)
                .ToList();
        }

        public async Task<List<Goal>> GetGoalsDueSoonAsync(Guid userId, int daysThreshold = 7, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            var dueDateThreshold = DateTime.UtcNow.AddDays(daysThreshold);

            return allGoals
                .Where(g => g.UserId == userId &&
                           !g.IsCompleted &&
                           g.DueDate <= dueDateThreshold &&
                           g.DueDate > DateTime.UtcNow)
                .ToList();
        }

        public async Task<int> GetTotalGoalsCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            return allGoals.Count(g => g.UserId == userId);
        }

        public async Task<Dictionary<GoalCategory, int>> GetGoalsCountByCategoryAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var allGoals = await base.GetAllAsync(cancellationToken);
            var userGoals = allGoals.Where(g => g.UserId == userId);

            return userGoals
                .GroupBy(g => g.Category)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
