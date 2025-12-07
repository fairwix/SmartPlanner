using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Achievements.Commands;

public class CheckAndAwardAchievementsCommandHandler :
    IRequestHandler<CheckAndAwardAchievementsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAchievementCheckerService _achievementChecker; // ✅ Специализированный сервис
    private readonly IMediator _mediator;

    public CheckAndAwardAchievementsCommandHandler(
        IApplicationDbContext context,
        IAchievementCheckerService achievementChecker,
        IMediator mediator)
    {
        _context = context;
        _achievementChecker = achievementChecker;
        _mediator = mediator;
    }

    public async Task Handle(
        CheckAndAwardAchievementsCommand request,
        CancellationToken cancellationToken)
    {
        // Используем специализированный сервис для проверки
        var eligibleAchievements = await _achievementChecker
            .CheckAndAwardEligibleAchievementsAsync(request.UserId, _context, cancellationToken);

        // Награждаем за каждое подходящее достижение
        foreach (var achievement in eligibleAchievements)
        {
            await _mediator.Send(new AwardAchievementCommand
            {
                UserId = request.UserId,
                AchievementId = achievement.Id
            }, cancellationToken);
        }
    }
}
