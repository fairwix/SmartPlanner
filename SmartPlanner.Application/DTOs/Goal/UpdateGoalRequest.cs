using System;
using System.ComponentModel.DataAnnotations;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.DTOs.Goal;

    public record UpdateGoalRequest(
        [StringLength(500)] string? Title,
        [StringLength(2000)] string? Description,
        GoalCategory? Category,
        GoalPriority? Priority,
        DateTime? DueDate,
        [Range(1, int.MaxValue)] int? TargetValue);

