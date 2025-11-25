// SmartPlanner.Application/Goals/Queries/GetGoalByIdQueryHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Queries
{
    public class GetGoalByIdQueryHandler : IRequestHandler<GetGoalByIdQuery, GoalDto?>
    {
        private readonly IGoalRepository _goalRepository;

        public GetGoalByIdQueryHandler(IGoalRepository goalRepository)
        {
            _goalRepository = goalRepository;
        }

        public async Task<GoalDto?> Handle(GetGoalByIdQuery request, CancellationToken cancellationToken)
        {
            var goal = await _goalRepository.GetByIdAsync(request.GoalId, cancellationToken);
            return goal != null ? MapToDto(goal) : null;
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