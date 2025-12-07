using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Interfaces.Services
{
    public interface IGoalService
    {
        Task<Goal?> GetGoalByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Goal>> GetUserGoalsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Goal> CreateGoalAsync(Goal goal, CancellationToken cancellationToken = default);
        Task UpdateGoalAsync(Goal goal, CancellationToken cancellationToken = default);
        Task DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> GoalExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
