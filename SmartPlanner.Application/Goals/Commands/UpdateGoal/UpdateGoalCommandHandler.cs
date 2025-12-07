using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces;

using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Goals.Commands
{
    public class UpdateGoalProgressCommandHandler : IRequestHandler<UpdateGoalProgressCommand, GoalDto?>
    {
        private readonly IApplicationDbContext _context;

        public UpdateGoalProgressCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GoalDto?> Handle(UpdateGoalProgressCommand request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == request.GoalId, cancellationToken);

            if (goal == null)
                return null;

            var oldValue = goal.CurrentValue;
            goal.CurrentValue = request.Value; // Используем прямое присваивание

            // Проверяем завершение цели
            if (!goal.IsCompleted && goal.CurrentValue >= goal.TargetValue)
            {
                goal.IsCompleted = true;

                // Награждаем пользователя
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == goal.UserId, cancellationToken);

                if (user != null)
                {
                    user.Balance += goal.RewardAmount;
                    _context.Users.Update(user);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(goal);
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
