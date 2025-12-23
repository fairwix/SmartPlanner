using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces;

public class AwardAchievementCommandHandler : IRequestHandler<AwardAchievementCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public AwardAchievementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(AwardAchievementCommand request, CancellationToken cancellationToken)
    {
        var achievement = await _context.Achievements
            .FirstOrDefaultAsync(a => a.Id == request.AchievementId, cancellationToken);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (achievement == null || user == null)
            return false;

        var alreadyExists = await _context.UserAchievements
            .AnyAsync(ua => ua.UserId == request.UserId &&
                            ua.AchievementId == request.AchievementId,
                cancellationToken);

        if (alreadyExists)
            return false;

        var userAchievement = new UserAchievement
        {
            UserId = request.UserId,
            AchievementId = request.AchievementId,
            AwardedAt = DateTime.UtcNow
        };

        await _context.UserAchievements.AddAsync(userAchievement, cancellationToken);

        user.AddReward(achievement.RewardAmount);
        _context.Users.Update(user);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
