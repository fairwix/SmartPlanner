using System;

namespace SmartPlanner.Domain.Entities;

    public class UserAchievement : BaseEntity
    {
        public Guid UserId { get; init; }
        public Guid AchievementId { get; init; }
        public DateTime AwardedAt { get; init; }

        public virtual User User { get; init; } = null!;
        public virtual Achievement Achievement { get; init; } = null!;
    }

