using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace SmartPlanner.Application.Goals.Commands
{
    public class UpdateGoalProgressCommandHandler : IRequestHandler<UpdateGoalProgressCommand, GoalDto?>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<UpdateGoalProgressCommandHandler> _logger;


        public UpdateGoalProgressCommandHandler(
            IApplicationDbContext context,
            ILogger<UpdateGoalProgressCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GoalDto?> Handle(UpdateGoalProgressCommand request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == request.GoalId, cancellationToken);

            if (goal == null)
                return null;

            try
            {
                goal.UpdateProgress(request.Value);

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Progress updated for goal {GoalId}: {CurrentValue}/{TargetValue}",
                    goal.Id, goal.CurrentValue, goal.TargetValue);

                return MapToDto(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId}", request.GoalId);
                throw;
            }
        }

        private GoalDto MapToDto(Goal goal) => new(
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
            goal.GetProgressPercentage(),
            goal.IsCompleted,
            goal.IsAiGenerated,
            goal.RewardAmount,
            goal.UserId,
            goal.IsExpired(),
            goal.IsOnTrack());
    }
}
