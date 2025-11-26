// SmartPlanner.Application/Goals/Commands/UpdateGoalCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;


namespace SmartPlanner.Application.Goals.Commands;

    public class UpdateGoalCommandHandler : IRequestHandler<UpdateGoalCommand, GoalDto?>
    {
        private readonly IGoalRepository _goalRepository;

        public UpdateGoalCommandHandler(IGoalRepository goalRepository)
        {
            _goalRepository = goalRepository;
        }

        public async Task<GoalDto?> Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _goalRepository.GetByIdAsync(request.GoalId, cancellationToken);
            if (goal == null)
                return null;

            // Create a new goal instance with updated values
            var updatedGoal = new Goal
            {
                Id = goal.Id,
                CreatedAt = goal.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                Title = !string.IsNullOrEmpty(request.Title) ? request.Title : goal.Title,
                Description = !string.IsNullOrEmpty(request.Description) ? request.Description : goal.Description,
                Category = !string.IsNullOrEmpty(request.Category) ? Enum.Parse<GoalCategory>(request.Category, true) : goal.Category,
                Priority = !string.IsNullOrEmpty(request.Priority) ? Enum.Parse<GoalPriority>(request.Priority, true) : goal.Priority,
                DueDate = request.DueDate ?? goal.DueDate,
                TargetValue = request.TargetValue ?? goal.TargetValue,
                CurrentValue = goal.CurrentValue,
                IsCompleted = goal.IsCompleted,
                IsAiGenerated = goal.IsAiGenerated,
                RewardAmount = goal.RewardAmount,
                UserId = goal.UserId,
                User = goal.User,
                ProgressHistory = goal.ProgressHistory
            };

            var savedGoal = await _goalRepository.UpdateAsync(updatedGoal, cancellationToken);
            return savedGoal != null ? MapToDto(savedGoal) : null;
        }

        private GoalDto MapToDto(Domain.Entities.Goal goal) => new(
                goal.Id,
                goal.CreatedAt,
                goal.UpdatedAt,
                goal.Title,
                goal.Description,
                goal.Category.ToString(), // Using ToString() directly as these are enums
                goal.Priority.ToString(),
                goal.DueDate,
                goal.TargetValue,
                goal.CurrentValue,
                goal.ProgressPercentage,
                goal.IsCompleted,
                goal.IsAiGenerated,
                goal.RewardAmount,
                goal.UserId,
                goal.IsExpired(),
                goal.IsOnTrack());
    }
