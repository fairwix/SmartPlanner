using System;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.DTOs.Goal;

    public record GoalResponse(
        Guid Id,
        string Title,
        string Description,
        GoalCategory Category,
        GoalPriority Priority,
        DateTime DueDate,
        int TargetValue,
        int CurrentValue,
        double ProgressPercentage,
        bool IsCompleted,
        bool IsAiGenerated,
        int RewardAmount,
        Guid UserId,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsExpired,
        bool IsOnTrack);

    public record UpdateProgressRequest(int Value);

