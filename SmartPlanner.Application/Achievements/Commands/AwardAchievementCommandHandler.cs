using MediatR;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

public class AwardAchievementCommandHandler : IRequestHandler<AwardAchievementCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork; // ✅ ТОЛЬКО Unit of Work

    public AwardAchievementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(AwardAchievementCommand request, CancellationToken cancellationToken)
    {
        // ✅ Все через Unit of Work
        var achievement = await _unitOfWork.Achievements.GetByIdAsync(request.AchievementId, cancellationToken);
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        
        if (achievement == null || user == null)
            return false;

        // Проверяем, не получено ли уже достижение
        var userAchievements = await _unitOfWork.UserAchievements.GetByUserIdAsync(request.UserId, cancellationToken);
        if (userAchievements.Any(ua => ua.AchievementId == request.AchievementId))
            return false;

        // Создаем запись о достижении
        var userAchievement = new UserAchievement
        {
            UserId = request.UserId,
            AchievementId = request.AchievementId,
            AwardedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserAchievements.CreateAsync(userAchievement, cancellationToken);

        // Награждаем пользователя
        user.AddReward(achievement.RewardAmount);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        
        // ✅ Все изменения в одной транзакции
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}