using System;
using FluentMigrator;

[Migration(0002)]
public class SeedData : Migration
{
    public override void Up()
    {
        Insert.IntoTable("Roles")
            .Row(new
            {
                Id = 1,
                Name = "Admin",
                NormalizedName = "ADMIN",
                CreatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = 2,
                Name = "User",
                NormalizedName = "USER",
                CreatedAt = DateTime.UtcNow
            });

        Insert.IntoTable("Users")
            .Row(new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "admin",
                Email = "admin@smartplanner.com",
                PasswordHash = "$2a$11$UzLiwMAWfVegWXiOD.XL4OSXKGzOaDFik8lsHgH9DGoeIEjlnIPwm",
                Balance = 1000,
                StreakCount = 7,
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Username = "testuser",
                Email = "user@example.com",
                PasswordHash = "$2a$11$UzLiwMAWfVegWXiOD.XL4OSXKGzOaDFik8lsHgH9DGoeIEjlnIPwm", // hashed "user123"
                Balance = 500,
                StreakCount = 3,
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Username = "john_doe",
                Email = "john.doe@example.com",
                PasswordHash = "$2a$11$UzLiwMAWfVegWXiOD.XL4OSXKGzOaDFik8lsHgH9DGoeIEjlnIPwm",
                Balance = 250,
                StreakCount = 14,
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });


        Insert.IntoTable("UserRoles")
            .Row(new
            {
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                RoleId = 1,
                CreatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                RoleId = 2,
                CreatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                RoleId = 2,
                CreatedAt = DateTime.UtcNow
            });


        Insert.IntoTable("Achievements")
            .Row(new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "First Steps",
                Description = "Complete your first goal",
                BadgeImage = "/badges/first-steps.png",
                RewardAmount = 50,
                Type = 1,
                Condition = "goals_completed:1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Name = "Week Streak",
                Description = "Maintain a 7-day streak",
                BadgeImage = "/badges/week-streak.png",
                RewardAmount = 100,
                Type = 0,
                Condition = "streak:7",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Name = "Social Butterfly",
                Description = "Make 5 friends",
                BadgeImage = "/badges/social-butterfly.png",
                RewardAmount = 150,
                Type = 2,
                Condition = "friends:5",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Name = "Goal Master",
                Description = "Complete 10 goals",
                BadgeImage = "/badges/goal-master.png",
                RewardAmount = 200,
                Type = 1,
                Condition = "goals_completed:10",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });


        Insert.IntoTable("Goals")
            .Row(new
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Title = "Learn ASP.NET Core",
                Description = "Complete ASP.NET Core Web API course",
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Category = 1,
                Priority = 2,
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                CurrentValue = 30,
                IsCompleted = false,
                IsAiGenerated = false,
                RewardAmount = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                Title = "30-Day Fitness Challenge",
                Description = "Exercise daily for 30 days",
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Category = 0,
                Priority = 1,
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 30,
                CurrentValue = 15,
                IsCompleted = false,
                IsAiGenerated = true,
                RewardAmount = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        Insert.IntoTable("Challenges")
            .Row(new
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                Title = "Spring Fitness Marathon",
                Description = "Complete 10,000 steps daily for 30 days",
                Type = 0,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(25),
                IsGroupChallenge = true,
                TargetValue = 300000,
                CurrentValue = 50000,
                CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                Title = "Reading Challenge 2025",
                Description = "Read 12 books in 2025",
                Type = 1,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                IsGroupChallenge = false,
                TargetValue = 12,
                CurrentValue = 3,
                CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        Insert.IntoTable("ChallengeParticipants")
            .Row(new
            {
                ChallengeId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Status = 1,
                JoinedAt = DateTime.UtcNow,
                PersonalContribution = 25000,
                CreatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                ChallengeId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Status = 1,
                JoinedAt = DateTime.UtcNow,
                PersonalContribution = 25000,
                CreatedAt = DateTime.UtcNow
            });

        Insert.IntoTable("UserFriends")
            .Row(new
            {
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                FriendId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Status = 1,
                CreatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                FriendId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Status = 1,
                CreatedAt = DateTime.UtcNow
            });

        Insert.IntoTable("UserAchievements")
            .Row(new
            {
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                AchievementId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                AwardedAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow
            })
            .Row(new
            {
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                AchievementId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                AwardedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow
            });
    }

    public override void Down()
    {
        Delete.FromTable("UserAchievements").AllRows();
        Delete.FromTable("UserFriends").AllRows();
        Delete.FromTable("ChallengeParticipants").AllRows();
        Delete.FromTable("Challenges").AllRows();
        Delete.FromTable("GoalProgress").AllRows();
        Delete.FromTable("Goals").AllRows();
        Delete.FromTable("Achievements").AllRows();
        Delete.FromTable("UserRoles").AllRows();
        Delete.FromTable("Users").AllRows();
        Delete.FromTable("Roles").AllRows();
    }
}
