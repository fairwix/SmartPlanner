using MediatR;
using SmartPlanner.Application.Users.Dtos;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Queries;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        // Загружаем пользователя с его интересами
        var user = await _context.Users
            .Include(u => u.UserInterests)          // Включаем UserInterests
            .ThenInclude(ui => ui.Interest)         // И связанные Interest
            .AsNoTracking()                         // Только для чтения
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return null;

        return MapToDto(user);
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
            user.LastLoginAt);
    }
}
