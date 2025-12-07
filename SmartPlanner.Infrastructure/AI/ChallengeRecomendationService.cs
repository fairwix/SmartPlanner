using MediatR;
using SmartPlanner.Application.AI.Queries;
using SmartPlanner.Application.Challenges.Dtos;

namespace SmartPlanner.Infrastructure.AI;

public class ChallengeRecommendationService
{
    private readonly IMediator _mediator; // ← Используем IMediator

    public ChallengeRecommendationService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<ChallengeDto>> GenerateSmartChallengesAsync(
        Guid userId,
        int count = 3,
        CancellationToken cancellationToken = default)
    {
        var query = new GeneratePersonalChallengesQuery { UserId = userId, Count = count };
        return await _mediator.Send(query, cancellationToken); // ← Отправляем через MediatR
    }
}
