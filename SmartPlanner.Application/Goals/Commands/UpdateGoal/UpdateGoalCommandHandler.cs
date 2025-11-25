// SmartPlanner.Application/Goals/Commands/UpdateGoalCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Goals.Commands
{
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

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Title))
                goal.Title = request.Title;

            if (!string.IsNullOrEmpty(request.Description))
                goal.Description = request.Description;

            if (!string.IsNullOrEmpty(request.Category))
                goal.Category = Enum.Parse<GoalCategory>(request.Category);

            if (!string.IsNullOrEmpty(request.Priority))
                goal.Priority = Enum.Parse<GoalPriority>(request.Priority);

            if (request.DueDate.HasValue)
                goal.DueDate = request.DueDate.Value;

            if (request.TargetValue.HasValue)
                goal.TargetValue = request.TargetValue.Value;

            goal.UpdatedAt = DateTime.UtcNow;

            var updatedGoal = await _goalRepository.UpdateAsync(goal, cancellationToken);
            return updatedGoal != null ? MapToDto(updatedGoal) : null;
        }

        private GoalDto MapToDto(Domain.Entities.Goal goal)
        {
            return new GoalDto
            {
                Id = goal.Id,
                Title = goal.Title,
                Description = goal.Description,
                Category = goal.Category.ToString(),
                Priority = goal.Priority.ToString(),
                DueDate = goal.DueDate,
                TargetValue = goal.TargetValue,
                CurrentValue = goal.CurrentValue,
                ProgressPercentage = goal.ProgressPercentage,
                IsCompleted = goal.IsCompleted,
                IsAiGenerated = goal.IsAiGenerated,
                RewardAmount = goal.RewardAmount,
                UserId = goal.UserId,
                CreatedAt = goal.CreatedAt,
                UpdatedAt = goal.UpdatedAt,
                IsExpired = goal.IsExpired(),
                IsOnTrack = goal.IsOnTrack()
            };
        }
    }
}