using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartPlanner.Domain.Entities;

public class User : BaseEntity
{
    // Изменяем init на set для поддержки обновления
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public virtual List<UserInterest> UserInterests { get; set; } = new List<UserInterest>();


    // ПЕРЕНЕСЕНО ИЗ ProjectState.UserBalance
    public int Balance { get; set; } = 0;

    public DateTime LastLogin { get; set; }
    public int StreakCount { get; set; } = 0;

    // Навигационные свойства
    public virtual List<Goal> Goals { get; init; } = new List<Goal>();
    public virtual List<UserFriend> Friends { get; init; } = new List<UserFriend>();
    public virtual List<UserAchievement> Achievements { get; init; } = new List<UserAchievement>();
    public virtual List<Challenge> CreatedChallenges { get; init; } = new List<Challenge>();
    public virtual List<ChallengeParticipant> ChallengeParticipants { get; init; } = new List<ChallengeParticipant>();

    // ПЕРЕНЕСЕНО ИЗ HomeController.HandleAction (логика начисления баллов)
    public void AddReward(int amount)
    {
        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    // НОВАЯ БИЗНЕС-ЛОГИКА
    public bool CanAfford(int price) => Balance >= price;

    public void UpdateStreak()
    {
        StreakCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetStreak()
    {
        StreakCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasInterest(string interest)
    {
        return UserInterests?.Any(ui =>
            ui.Interest != null &&
            ui.Interest.Name.Equals(interest, StringComparison.OrdinalIgnoreCase)
        ) ?? false;
    }

    public bool CanJoinChallenge(Challenge challenge)
    {
        return challenge.IsActive() &&
               !challenge.Participants.Any(p => p.UserId == Id && p.Status == ParticipantStatus.Joined);
    }

    public List<string> GetInterests()
    {
        return UserInterests?.Select(ui => ui.Interest.Name).ToList() ?? new List<string>();
    }

}
