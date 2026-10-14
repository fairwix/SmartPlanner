using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Achievements.Queries;

public class GetAchievementByIdQueryHandler : IRequestHandler<GetAchievementByIdQuery, AchievementDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAchievementByIdQueryHandler(
        IApplicationDbContext context,
        IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AchievementDto?> Handle(
        GetAchievementByIdQuery request,
        CancellationToken cancellationToken)
    {
        var achievementDto = await _context.Achievements
            .AsNoTracking()
            .Where(a => a.Id == request.AchievementId)
            .Select(a => new AchievementDto(
                a.Id,
                a.CreatedAt,
                a.UpdatedAt,
                a.Name,
                a.Description,
                a.BadgeImage,
                a.RewardAmount,
                a.Type.ToString(),
                a.Condition))
            .FirstOrDefaultAsync(cancellationToken);

        return achievementDto;
    }
}
