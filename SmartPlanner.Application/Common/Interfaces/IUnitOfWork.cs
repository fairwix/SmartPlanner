// Application/Common/Interfaces/IUnitOfWork.cs

using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Application.Common.Interfaces
{
    public interface IUnitOfWork
    {
        // ТОЛЬКО специфичные репозитории
        IUserRepository Users { get; }
        IGoalRepository Goals { get; }
        IChallengeRepository Challenges { get; }
        IAchievementRepository Achievements { get; }
        IUserAchievementRepository UserAchievements { get; }

        // ТОЛЬКО координация транзакций
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}