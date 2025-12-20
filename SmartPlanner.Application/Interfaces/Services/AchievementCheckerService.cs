using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;

namespace SmartPlanner.Application.Services;

public class AchievementCheckerService : IAchievementCheckerService
{
    public async Task<List<Achievement>> CheckAndAwardEligibleAchievementsAsync(
        Guid userId,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // ✅ Оптимизированный запрос: собираем всю статистику за ОДИН запрос
        var userStats = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                User = u,
                CompletedGoalsCount = u.Goals.Count(g => g.IsCompleted),
                FriendsCount = u.Friends.Count(f => f.Status == FriendStatus.Accepted),
                UserAchievementIds = u.Achievements.Select(a => a.AchievementId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (userStats == null) return new List<Achievement>();

        var allAchievements = await context.Achievements
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var eligibleAchievements = new List<Achievement>();

        foreach (var achievement in allAchievements)
        {
            // Проверяем, не получено ли уже
            if (userStats.UserAchievementIds.Contains(achievement.Id))
                continue;

            // Проверяем условие
            if (MeetsAchievementCondition(achievement, userStats.User,
                userStats.CompletedGoalsCount, userStats.FriendsCount))
            {
                eligibleAchievements.Add(achievement);
            }
        }

        return eligibleAchievements;
    }

    public bool MeetsAchievementCondition(Achievement achievement, User user, int completedGoalsCount, int friendsCount)
    {
        return achievement.Type switch
        {
            AchievementType.Streak => CheckStreakAchievement(achievement, user),
            AchievementType.GoalsCompleted => CheckGoalsCompletedAchievement(achievement, completedGoalsCount),
            AchievementType.Friends => CheckFriendsAchievement(achievement, friendsCount),
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
}
