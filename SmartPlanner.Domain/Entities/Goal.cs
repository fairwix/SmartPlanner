using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartPlanner.Domain.Entities;

public interface IUserOwnedResource
{
    Guid UserId { get; }
}

public class Goal : BaseEntity, IUserOwnedResource
{
    private string _title = string.Empty;
    private DateTime _dueDate;

    public string Title
    {
        get => _title;
        set => _title = !string.IsNullOrWhiteSpace(value) ? value :
            throw new ArgumentException("Title cannot be empty", nameof(value));
    }

    public string Description { get; set; } = string.Empty;
    public GoalCategory Category { get; set; }
    public GoalPriority Priority { get; set; }

    public DateTime DueDate
    {
        get => _dueDate;
        set => _dueDate = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    public int TargetValue { get; set; } = 1;
    public int CurrentValue { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
    public bool IsAiGenerated { get; init; } = false;
    public int RewardAmount { get; init; } = 10;
    public Guid UserId { get; init; }

    public virtual User User { get; init; } = null!;
    public virtual List<GoalProgress> ProgressHistory { get; init; } = new List<GoalProgress>();

    public double GetProgressPercentage() =>
        TargetValue > 0 ? (CurrentValue * 100.0) / TargetValue : 0;

    public int GetRemainingValue() => TargetValue - CurrentValue;

    public void UpdateProgress(int value)
    {
        if (value < 0 || value > TargetValue)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"Value must be between 0 and {TargetValue}");

        var oldValue = CurrentValue;
        CurrentValue = value;

        ProgressHistory.Add(new GoalProgress
        {
            Id = Guid.NewGuid(),
            GoalId = this.Id,
            Value = CurrentValue,
            PreviousValue = oldValue,
            CreatedAt = DateTime.UtcNow
        });

        if (CurrentValue >= TargetValue && !IsCompleted)
        {
            CompleteGoal();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private void CompleteGoal()
    {
        IsCompleted = true;
        if (User != null)
        {
            User.AddReward(RewardAmount);
        }
    }

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Title) || Title.Length > 500)
            return false;

        if (UserId == Guid.Empty)
            return false;

        if (DueDate == default)
            return false;

        if (TargetValue <= 0)
            return false;

        if (CurrentValue < 0 || CurrentValue > TargetValue)
            return false;

        return true;
    }

    public bool IsExpired() => DueDate < DateTime.UtcNow;

    public bool CanBeEdited() => !IsCompleted && !IsExpired();

    public bool IsOnTrack()
    {
        if (IsCompleted)
            return true;

        if (DueDate == DateTime.MinValue || DueDate <= CreatedAt)
            return false;

        var timePassed = (DateTime.UtcNow - CreatedAt).TotalDays;
        if (timePassed <= 0)
            return true;

        var totalTime = (DueDate - CreatedAt).TotalDays;
        if (totalTime <= 0)
            return false;

        var expectedProgress = (timePassed / totalTime) * 100;
        return GetProgressPercentage() >= expectedProgress * 0.9;
    }
}
