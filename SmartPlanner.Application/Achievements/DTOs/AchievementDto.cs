// SmartPlanner.Application/Achievements/Dtos/AchievementDto.cs

using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Achievements.Dtos
{
    public class AchievementDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BadgeImage { get; set; } = string.Empty;
        public int RewardAmount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
    }

    public class UserAchievementDto : BaseDto
    {
        public Guid UserId { get; set; }
        public Guid AchievementId { get; set; }
        public string AchievementName { get; set; } = string.Empty;
        public string AchievementDescription { get; set; } = string.Empty;
        public string BadgeImage { get; set; } = string.Empty;
        public DateTime AwardedAt { get; set; }
    }
}