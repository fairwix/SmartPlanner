// SmartPlanner.Application/Goals/Dtos/GoalDto.cs

using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Goals.Dtos;

    public record GoalDto(
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Title,
        string Description,
        string Category,
        string Priority,
        DateTime DueDate,
        int TargetValue,
        int CurrentValue,
        double ProgressPercentage,
        bool IsCompleted,
        bool IsAiGenerated,
        int RewardAmount,
        Guid UserId,
        bool IsExpired,
        bool IsOnTrack) : BaseDto(Id, CreatedAt, UpdatedAt);

    public record CreateGoalDto(
        string Title,
        string Description,
        string Category,
        string Priority,
        DateTime DueDate,
        int TargetValue,
        Guid UserId,
        bool IsAiGenerated = false,
        int RewardAmount = 10);

    public record UpdateGoalDto(
        string? Title,
        string? Description,
        string? Category,
        string? Priority,
        DateTime? DueDate,
        int? TargetValue);

    public record UpdateGoalProgressDto(int Value);
