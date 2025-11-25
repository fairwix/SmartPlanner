using System;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Domain.DTOs.Goal
{
    public class CreateGoalRequest
    {
        public string Title { get; set; } = string.Empty;        // ПЕРЕНЕСЕНО: бывшее "message"
        public string Description { get; set; } = string.Empty;
        public GoalCategory Category { get; set; }
        public GoalPriority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public int TargetValue { get; set; } = 1;
        public Guid UserId { get; set; }
    }
}