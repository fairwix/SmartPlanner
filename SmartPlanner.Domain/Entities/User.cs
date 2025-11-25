using System;
using System.Collections.Generic;

namespace SmartPlanner.Domain.Entities

{
    public class User : BaseEntity
    {
        // НОВЫЕ ПОЛЯ
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        
        // ПЕРЕНЕСЕНО ИЗ ProjectState.UserBalance
        public int Balance { get; set; } = 0;
        
        // НОВЫЕ ПОЛЯ для ИИ-рекомендаций и социальных функций
        public List<string> Interests { get; set; } = new List<string>();
        public DateTime LastLogin { get; set; }
        public int StreakCount { get; set; } = 0;
        
        // Навигационные свойства
        public virtual List<Goal> Goals { get; set; } = new List<Goal>();
        public virtual List<UserFriend> Friends { get; set; } = new List<UserFriend>();
        public virtual List<UserAchievement> Achievements { get; set; } = new List<UserAchievement>();
        public virtual List<Challenge> CreatedChallenges { get; set; } = new List<Challenge>();
        public virtual List<ChallengeParticipant> ChallengeParticipants { get; set; } = new List<ChallengeParticipant>();

        // ПЕРЕНЕСЕНО ИЗ HomeController.HandleAction (логика начисления баллов)
        public void AddReward(int amount)
        {
            // Было: _state.UserBalance += 10;
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

        public bool HasInterest(string interest) => Interests.Contains(interest);

        public bool CanJoinChallenge(Challenge challenge)
        {
            return challenge.IsActive && 
                   !challenge.Participants.Any(p => p.UserId == Id && p.Status == ParticipantStatus.Joined);
        }
    }
}