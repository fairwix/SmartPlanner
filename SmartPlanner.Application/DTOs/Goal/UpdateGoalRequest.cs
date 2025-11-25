using System;
using System.ComponentModel.DataAnnotations;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Domain.DTOs.Goal
{
    public class UpdateGoalRequest
    {
        [StringLength(500)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public GoalCategory? Category { get; set; }

        public GoalPriority? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? TargetValue { get; set; }
    }
}