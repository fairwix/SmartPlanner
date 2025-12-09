using MediatR;
using SmartPlanner.Application.Users.Dtos;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Queries;

public class GetUserFriendsQueryHandler : IRequestHandler<GetUserFriendsQuery, List<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserFriendsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> Handle(GetUserFriendsQuery request, CancellationToken cancellationToken)
    {
        // Загружаем друзей с их интересами через UserFriends
        var friendIds = await _context.UserFriends
            .AsNoTracking()
            .Where(uf => uf.UserId == request.UserId && uf.Status == FriendStatus.Accepted)
            .Select(uf => uf.FriendId)
            .ToListAsync(cancellationToken);

        if (!friendIds.Any())
            return new List<UserDto>();

        // Загружаем полную информацию о друзьях с их интересами
        var friends = await _context.Users
            .Include(u => u.UserInterests)          // Включаем UserInterests
            .ThenInclude(ui => ui.Interest)         // И связанные Interest
            .Where(u => friendIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return friends.Select(MapToDto).ToList();
    }

    // Альтернативный вариант с одним запросом (JOIN):
    public async Task<List<UserDto>> Handle_Alternative(GetUserFriendsQuery request, CancellationToken cancellationToken)
    {
        var friends = await _context.UserFriends
            .Include(uf => uf.Friend)               // Включаем Friend
            .ThenInclude(f => f.UserInterests)      // И его UserInterests
            .ThenInclude(ui => ui.Interest)         // И связанные Interest
            .Where(uf => uf.UserId == request.UserId && uf.Status == FriendStatus.Accepted)
            .Select(uf => uf.Friend)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return friends.Select(MapToDto).ToList();
    }

    private UserDto MapToDto(User user)
    {
        // Получаем имена интересов из UserInterests
        var interests = user.UserInterests
            .Where(ui => ui.Interest != null)
            .Select(ui => ui.Interest.Name)
            .ToList();

        return new UserDto(
            user.Id,
            user.CreatedAt,
            user.UpdatedAt,
            user.Username,
            user.Email,
            interests,
            user.Balance,
            user.StreakCount,
            user.LastLogin);
    }
}
