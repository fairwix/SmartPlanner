// SmartPlanner.Application/Goals/Dtos/GoalDto.cs

using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Goals.Dtos
{
    public class GoalDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int TargetValue { get; set; }
        public int CurrentValue { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsAiGenerated { get; set; }
        public int RewardAmount { get; set; }
        public Guid UserId { get; set; }
        public bool IsExpired { get; set; }
        public bool IsOnTrack { get; set; }
    }

    public class CreateGoalDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int TargetValue { get; set; } = 1;
        public Guid UserId { get; set; }
    }

    public class UpdateGoalDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public int? TargetValue { get; set; }
    }

    public class UpdateGoalProgressDto
    {
        public int Value { get; set; }
    }
}