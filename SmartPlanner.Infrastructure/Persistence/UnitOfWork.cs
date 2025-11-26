using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Infrastructure.Persistence;

    public class UnitOfWork : IUnitOfWork
    {

        private readonly IUserRepository _userRepository;
        private readonly IGoalRepository _goalRepository;
        private readonly IAchievementRepository _achievementRepository;
        private readonly IChallengeRepository _challengeRepository;
        private readonly IUserAchievementRepository _userAchievementRepository;

        public UnitOfWork(IUserRepository userRepository, IGoalRepository goalRepository, IAchievementRepository achievementRepository, IChallengeRepository challengeRepository, IUserAchievementRepository userAchievementRepository)
        {
            _userRepository = userRepository;
            _goalRepository = goalRepository;
            _achievementRepository = achievementRepository;
            _challengeRepository = challengeRepository;
            _userAchievementRepository = userAchievementRepository;
        }

        public IUserRepository Users => _userRepository;
        public IGoalRepository Goals => _goalRepository;
        public IAchievementRepository Achievements => _achievementRepository;
        public IChallengeRepository Challenges => _challengeRepository;
        public IUserAchievementRepository UserAchievements => _userAchievementRepository;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // В файловом хранилище изменения сохраняются немедленно при операциях Create/Update/Delete
            // Поэтому здесь просто возвращаем успешный результат
            return await Task.FromResult(1);
        }
    }

