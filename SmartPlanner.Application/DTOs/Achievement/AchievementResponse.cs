using System;
using SmartPlanner.Domain.Entities;


namespace SmartPlanner.Domain.DTOs.Achievement
{
    public class AchievementResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BadgeImage { get; set; } = string.Empty;
        public int RewardAmount { get; set; }
        public AchievementType Type { get; set; }
        public string Condition { get; set; } = string.Empty;
    }

    public class UserAchievementResponse
    {
        public Guid AchievementId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BadgeImage { get; set; } = string.Empty;
        public DateTime AwardedAt { get; set; }
    }
}