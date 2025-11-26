// SmartPlanner.Application/Goals/Commands/UpdateGoalProgressCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Commands;

    public class UpdateGoalProgressCommandHandler : IRequestHandler<UpdateGoalProgressCommand, GoalDto?>
    {
        private readonly IGoalRepository _goalRepository;
        private readonly IUserRepository _userRepository;

        public UpdateGoalProgressCommandHandler(IGoalRepository goalRepository, IUserRepository userRepository)
        {
            _goalRepository = goalRepository;
            _userRepository = userRepository;
        }

        public async Task<GoalDto?> Handle(UpdateGoalProgressCommand request, CancellationToken cancellationToken)
        {
            var goal = await _goalRepository.GetByIdAsync(request.GoalId, cancellationToken);
            if (goal == null)
                return null;

            var oldValue = goal.CurrentValue;
            goal.UpdateProgress(request.Value);

            var updatedGoal = await _goalRepository.UpdateAsync(goal, cancellationToken);

            // Award user if goal completed
            if (goal.IsCompleted && oldValue < goal.TargetValue)
            {
                var user = await _userRepository.GetByIdAsync(goal.UserId, cancellationToken);
                if (user != null)
                {
                    user.AddReward(goal.RewardAmount);
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }
            }

            return updatedGoal != null ? MapToDto(updatedGoal) : null;
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
