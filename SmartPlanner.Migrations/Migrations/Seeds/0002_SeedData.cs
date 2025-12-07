// SmartPlanner.Infrastructure/Migrations/0002_SeedData.cs
using System;
using FluentMigrator;

namespace SmartPlanner.Infrastructure.Migrations
{
    [Migration(0002)]
    public class SeedData : Migration
    {
        public override void Up()
        {
            // Insert default roles
            Insert.IntoTable("Users")
                .Row(new
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Username = "admin",
                    Email = "admin@smartplanner.com",
                    PasswordHash = "AQAAAAIAAYagAAAAEHYZ1wJ3XKJk4XzXJ9X8Q==", // hashed "admin123"
                    Balance = 1000,
                    StreakCount = 7,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .Row(new
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Username = "testuser",
                    Email = "user@example.com",
                    PasswordHash = "AQAAAAIAAYagAAAAEHYZ1wJ3XKJk4XzXJ9X8Q==", // hashed "user123"
                    Balance = 500,
                    StreakCount = 3,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            // Insert sample achievements
            Insert.IntoTable("Achievements")
                .Row(new
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Name = "First Steps",
                    Description = "Complete your first goal",
                    BadgeImage = "badges/first-steps.png",
                    RewardAmount = 50,
                    Type = 1, // GoalsCompleted
                    Condition = "goals_completed:1",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .Row(new
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Name = "Week Streak",
                    Description = "Maintain a 7-day streak",
                    BadgeImage = "badges/week-streak.png",
                    RewardAmount = 100,
                    Type = 0, // Streak
                    Condition = "streak:7",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            // Insert sample challenge
            Insert.IntoTable("Challenges")
                .Row(new
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Title = "30-Day Fitness Challenge",
                    Description = "Complete your fitness goals for 30 days",
                    Type = 2, // Exercise
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(30),
                    IsGroupChallenge = true,
                    TargetValue = 30,
                    CurrentValue = 0,
                    CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000001"), // admin
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
        }

        public override void Down()
        {
            // Delete the seeded data in reverse order
            Delete.FromTable("Challenges").AllRows();
            Delete.FromTable("Achievements").AllRows();
            Delete.FromTable("Users").AllRows();
        }
    }
}
