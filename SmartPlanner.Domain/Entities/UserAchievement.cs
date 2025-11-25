using System;

namespace SmartPlanner.Domain.Entities
{
    public class UserAchievement : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid AchievementId { get; set; }
        public DateTime AwardedAt { get; set; }
        
        public virtual User User { get; set; } = null!;
        public virtual Achievement Achievement { get; set; } = null!;
    }
}