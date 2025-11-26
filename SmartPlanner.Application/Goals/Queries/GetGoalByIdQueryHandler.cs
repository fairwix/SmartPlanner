// SmartPlanner.Application/Goals/Queries/GetGoalByIdQueryHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Queries;

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
            return new GoalDto(
                goal.Id,
                goal.CreatedAt,
                goal.UpdatedAt,
                goal.Title,
                goal.Description,
                goal.Category.ToString(),
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
    }
