using System;
using SmartPlanner.Domain.Entities;


namespace SmartPlanner.Application.DTOs.Goal;

    public record CreateGoalRequest(
        string Title,
        string Description,
        GoalCategory Category,
        GoalPriority Priority,
        DateTime DueDate,
        int TargetValue,
        Guid UserId);
