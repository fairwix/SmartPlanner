using SmartPlanner.Domain.Entities;

public class UserAchievement
{
    public Guid Id { get; set; }
    public Guid UserId { get; init; }
    public Guid AchievementId { get; init; }
    public DateTime AwardedAt { get; init; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; init; } = null!;
    public virtual Achievement Achievement { get; init; } = null!;
}
