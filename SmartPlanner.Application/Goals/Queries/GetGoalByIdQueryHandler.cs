using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Goals.Queries
{
    public class GetGoalByIdQueryHandler : IRequestHandler<GetGoalByIdQuery, GoalDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetGoalByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GoalDto?> Handle(GetGoalByIdQuery request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == request.GoalId, cancellationToken);

            return goal != null ? MapToDto(goal) : null;
        }

        private GoalDto MapToDto(Goal goal)
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
                goal.GetProgressPercentage(),
                goal.IsCompleted,
                goal.IsAiGenerated,
                goal.RewardAmount,
                goal.UserId,
                goal.IsExpired(),
                goal.IsOnTrack());
        }
    }
}
