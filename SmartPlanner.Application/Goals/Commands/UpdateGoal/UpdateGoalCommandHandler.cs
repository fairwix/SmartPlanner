using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces;
using Microsoft.Extensions.Logging; // Добавляем логирование

namespace SmartPlanner.Application.Goals.Commands
{
    public class UpdateGoalProgressCommandHandler : IRequestHandler<UpdateGoalProgressCommand, GoalDto?>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<UpdateGoalProgressCommandHandler> _logger;

        // ✅ ДОБАВЛЯЕМ ЛОГГЕР
        public UpdateGoalProgressCommandHandler(
            IApplicationDbContext context,
            ILogger<UpdateGoalProgressCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GoalDto?> Handle(UpdateGoalProgressCommand request, CancellationToken cancellationToken)
        {
            // ✅ 1. ЗАГРУЖАЕМ ЦЕЛЬ С ПОЛЬЗОВАТЕЛЕМ И ИСТОРИЕЙ
            var goal = await _context.Goals
                .Include(g => g.User) // ⚠️ ВАЖНО: Include для User!
                .Include(g => g.ProgressHistory) // опционально, если нужно
                .FirstOrDefaultAsync(g => g.Id == request.GoalId, cancellationToken);

            if (goal == null)
            {
                _logger.LogWarning("Goal with ID {GoalId} not found", request.GoalId);
                return null;
            }

            try
            {
                // ✅ 2. ИСПОЛЬЗУЕМ ДОМЕННЫЙ МЕТОД ВМЕСТО ПРЯМОГО ПРИСВАИВАНИЯ
                goal.UpdateProgress(request.Value);

                // ✅ 3. User уже обновлён внутри CompleteGoal() - не нужно вручную

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Progress updated for goal {GoalId}: {CurrentValue}/{TargetValue}",
                    goal.Id, goal.CurrentValue, goal.TargetValue);

                return MapToDto(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId}", request.GoalId);
                throw; // Пробрасываем дальше
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
