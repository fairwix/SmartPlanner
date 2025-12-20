using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Tests.TestData
{
    public static class TestDataFactory
    {
        public static User CreateTestUser(Guid? id = null, string username = "testuser")
        {
            return new User
            {
                Id = id ?? Guid.NewGuid(),
                Username = username,
                Email = $"{username}@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Balance = 100,
                StreakCount = 5,
                LastLoginAt = DateTime.UtcNow
            };
        }

        public static Goal CreateTestGoal(Guid userId, Guid? goalId = null, bool isCompleted = false)
        {
            return new Goal
            {
                Id = goalId ?? Guid.NewGuid(),
                Title = "Test Goal",
                Description = "Test Description",
                Category = GoalCategory.Sports,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                CurrentValue = isCompleted ? 100 : 30,
                IsCompleted = isCompleted,
                IsAiGenerated = false,
                RewardAmount = 50,
                UserId = userId
            };
        }

        public static Achievement CreateTestAchievement(
            Guid? id = null,
            AchievementType type = AchievementType.Streak,
            string condition = "streak:7")
        {
            return new Achievement
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Achievement",
                Description = "Test Description",
                BadgeImage = "/badges/test.png",
                RewardAmount = 100,
                Type = type,
                Condition = condition
            };
        }

        public static Challenge CreateTestChallenge(Guid createdBy, bool isActive = true)
        {
            var now = DateTime.UtcNow;
            return new Challenge
            {
                Id = Guid.NewGuid(),
                Title = "Test Challenge",
                Description = "Test Description",
                Type = ChallengeType.Exercise,
                StartDate = isActive ? now.AddDays(-1) : now.AddDays(1),
                EndDate = isActive ? now.AddDays(7) : now.AddDays(2),
                IsGroupChallenge = true,
                TargetValue = 1000,
                CurrentValue = 300,
                CreatedBy = createdBy
            };
        }
    }
}
