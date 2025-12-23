using System.Collections.Generic;

namespace SmartPlanner.Domain.Entities;

public class Achievement : BaseEntity
{
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string BadgeImage { get; init; } = string.Empty;
        public int RewardAmount { get; init; }
        public AchievementType Type { get; init; }
        public string Condition { get; init; } = string.Empty;

        public virtual ICollection<UserAchievement> UserAchievements { get; init; } = new List<UserAchievement>();

        public bool CanBeAwarded(User user)
        {
            return Type switch
            {
                AchievementType.Streak => CheckStreakCondition(user),
                AchievementType.GoalsCompleted => CheckGoalsCompletedCondition(user),
                AchievementType.Friends => CheckFriendsCondition(user),
                _ => false
            };
        }

        private bool CheckStreakCondition(User user)
        {
            if (int.TryParse(Condition.Replace("streak:", ""), out int requiredStreak))
            {
                return user.StreakCount >= requiredStreak;
            }
            return false;
        }

        private bool CheckGoalsCompletedCondition(User user)
        {
            if (int.TryParse(Condition.Replace("goals_completed:", ""), out int requiredGoals))
            {
                return user.Goals.Count(g => g.IsCompleted) >= requiredGoals;
            }
            return false;
        }

        private bool CheckFriendsCondition(User user)
        {
            if (int.TryParse(Condition.Replace("friends:", ""), out int requiredFriends))
            {
                return user.Friends.Count(f => f.Status == FriendStatus.Accepted) >= requiredFriends;
            }
            return false;
        }
    }

