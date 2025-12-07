using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces;

public class AwardAchievementCommandHandler : IRequestHandler<AwardAchievementCommand, bool>
{
    private readonly IApplicationDbContext _context; // ✅ Прямой доступ

    public AwardAchievementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(AwardAchievementCommand request, CancellationToken cancellationToken)
    {
        // ✅ Все через DbContext напрямую
        var achievement = await _context.Achievements
            .FirstOrDefaultAsync(a => a.Id == request.AchievementId, cancellationToken);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (achievement == null || user == null)
            return false;

        // Проверяем, не получено ли уже достижение
        var alreadyExists = await _context.UserAchievements
            .AnyAsync(ua => ua.UserId == request.UserId &&
                            ua.AchievementId == request.AchievementId,
                cancellationToken);

        if (alreadyExists)
            return false;

        // Создаем запись о достижении
        var userAchievement = new UserAchievement
        {
            UserId = request.UserId,
            AchievementId = request.AchievementId,
            AwardedAt = DateTime.UtcNow
        };

        await _context.UserAchievements.AddAsync(userAchievement, cancellationToken);

        // Награждаем пользователя
        user.AddReward(achievement.RewardAmount);
        _context.Users.Update(user);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
