using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Achievements.Dtos;

    public record AchievementDto(
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Name,
        string Description,
        string BadgeImage,
        int RewardAmount,
        string Type,
        string Condition) : BaseDto(Id, CreatedAt, UpdatedAt);

    public record UserAchievementDto(
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Guid UserId,
        Guid AchievementId,
        string AchievementName,
        string AchievementDescription,
        string BadgeImage,
        DateTime AwardedAt) : BaseDto(Id, CreatedAt, UpdatedAt);
