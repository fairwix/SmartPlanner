using System;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Domain.DTOs.Goal
{
    public class GoalResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GoalCategory Category { get; set; }
        public GoalPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public int TargetValue { get; set; }
        public int CurrentValue { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsAiGenerated { get; set; }
        public int RewardAmount { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsExpired { get; set; }
        public bool IsOnTrack { get; set; }
    }

    public class UpdateProgressRequest
    {
        public int Value { get; set; }
    }
}