// SmartPlanner.Application/Achievements/Commands/CheckAndAwardAchievementsCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;


namespace SmartPlanner.Application.Achievements.Commands;

    public class CheckAndAwardAchievementsCommandHandler : IRequestHandler<CheckAndAwardAchievementsCommand>
    {
        private readonly IAchievementRepository _achievementRepository;
        private readonly IUserAchievementRepository _userAchievementRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGoalRepository _goalRepository;
        private readonly IMediator _mediator;

        public CheckAndAwardAchievementsCommandHandler(
            IAchievementRepository achievementRepository,
            IUserAchievementRepository userAchievementRepository,
            IUserRepository userRepository,
            IGoalRepository goalRepository,
            IMediator mediator)
        {
            _achievementRepository = achievementRepository;
            _userAchievementRepository = userAchievementRepository;
            _userRepository = userRepository;
            _goalRepository = goalRepository;
            _mediator = mediator;
        }

        public async Task Handle(CheckAndAwardAchievementsCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null) return;

            var allAchievements = await _achievementRepository.GetAllAsync(cancellationToken);
            var userAchievements = await _userAchievementRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            var awardedAchievementIds = userAchievements.Select(ua => ua.AchievementId).ToHashSet();

            var userGoals = await _goalRepository.GetUserGoalsAsync(request.UserId, cancellationToken);
            var completedGoals = userGoals.Count(g => g.IsCompleted);
            var friends = await _userRepository.GetUserFriendsAsync(request.UserId, cancellationToken);

            foreach (var achievement in allAchievements)
            {
                if (awardedAchievementIds.Contains(achievement.Id))
                    continue;

                if (ShouldAwardAchievement(achievement, user, completedGoals, friends.Count))
                {
                    await _mediator.Send(new AwardAchievementCommand
                    {
                        UserId = request.UserId,
                        AchievementId = achievement.Id
                    }, cancellationToken);
                }
            }
        }

        private bool ShouldAwardAchievement(Achievement achievement, User user, int completedGoals, int friendsCount)
        {
            return achievement.Type switch
            {
                AchievementType.Streak => CheckStreakAchievement(achievement, user),
                AchievementType.GoalsCompleted => CheckGoalsCompletedAchievement(achievement, completedGoals),
                AchievementType.Friends => CheckFriendsAchievement(achievement, friendsCount),
                AchievementType.ChallengeCompletion => CheckChallengeCompletionAchievement(achievement, user),
                AchievementType.Social => CheckSocialAchievement(achievement, user),
                _ => false
            };
        }

        private bool CheckStreakAchievement(Achievement achievement, User user)
        {
            if (int.TryParse(achievement.Condition.Replace("streak:", ""), out int requiredStreak))
            {
                return user.StreakCount >= requiredStreak;
            }
            return false;
        }

        private bool CheckGoalsCompletedAchievement(Achievement achievement, int completedGoals)
        {
            if (int.TryParse(achievement.Condition.Replace("goals_completed:", ""), out int requiredGoals))
            {
                return completedGoals >= requiredGoals;
            }
            return false;
        }

        private bool CheckFriendsAchievement(Achievement achievement, int friendsCount)
        {
            if (int.TryParse(achievement.Condition.Replace("friends:", ""), out int requiredFriends))
            {
                return friendsCount >= requiredFriends;
            }
            return false;
        }

        private bool CheckChallengeCompletionAchievement(Achievement achievement, User user)
        {
            // Implementation for challenge completion achievements
            return false; // Placeholder
        }

        private bool CheckSocialAchievement(Achievement achievement, User user)
        {
            // Implementation for social achievements
            return false; // Placeholder
        }
    }
