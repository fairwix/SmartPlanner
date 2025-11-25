using Microsoft.Extensions.DependencyInjection;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWork(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IUserRepository Users => _serviceProvider.GetRequiredService<IUserRepository>();
        public IGoalRepository Goals => _serviceProvider.GetRequiredService<IGoalRepository>();
        public IAchievementRepository Achievements => _serviceProvider.GetRequiredService<IAchievementRepository>();
        public IChallengeRepository Challenges => _serviceProvider.GetRequiredService<IChallengeRepository>();
        public IUserAchievementRepository UserAchievements => _serviceProvider.GetRequiredService<IUserAchievementRepository>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }
}