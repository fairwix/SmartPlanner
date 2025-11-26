using System;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.DTOs.Achievement;

    public record AchievementResponse(
        Guid Id,
        string Name,
        string Description,
        string BadgeImage,
        int RewardAmount,
        AchievementType Type,
        string Condition);

    public record UserAchievementResponse(
        Guid AchievementId,
        string Name,
        string Description,
        string BadgeImage,
        DateTime AwardedAt);

