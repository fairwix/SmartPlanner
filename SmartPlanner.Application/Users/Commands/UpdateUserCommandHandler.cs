using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Commands;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto?> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // Загружаем пользователя с его текущими интересами
        var user = await _context.Users
            .Include(u => u.UserInterests)  // Включаем UserInterests
            .ThenInclude(ui => ui.Interest) // И связанные Interest
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return null;

        bool hasChanges = false;

        // Обновляем username, если он указан и изменился
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            // Проверяем уникальность username
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username && u.Id != request.UserId,
                         cancellationToken);

            if (usernameExists)
                throw new ArgumentException($"Username '{request.Username}' is already taken");

            user.Username = request.Username;
            hasChanges = true;
        }

        // Обновляем интересы, если они указаны
        if (request.Interests is not null)
        {
            // Получаем текущие интересы как список имен
            var currentInterestNames = user.UserInterests
                .Select(ui => ui.Interest.Name)
                .ToList();

            // Сравниваем с новыми интересами
            var newInterestNames = request.Interests
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct()
                .ToList();

            // Если интересы изменились
            if (!currentInterestNames.SequenceEqual(newInterestNames))
            {
                // Удаляем все существующие связи UserInterest
                var existingUserInterests = _context.UserInterests
                    .Where(ui => ui.UserId == request.UserId);
                _context.UserInterests.RemoveRange(existingUserInterests);

                // Добавляем новые связи
                foreach (var interestName in newInterestNames)
                {
                    // Находим или создаем интерес
                    var interest = await _context.Interests
                        .FirstOrDefaultAsync(i => i.Name.ToLower() == interestName.ToLower(),
                            cancellationToken);

                    if (interest == null)
                    {
                        interest = new Interest
                        {
                            Name = interestName,
                            Description = null,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _context.Interests.AddAsync(interest, cancellationToken);

                        // Нужно сохранить, чтобы получить Id
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    // Создаем связь пользователя с интересом
                    var userInterest = new UserInterest
                    {
                        UserId = user.Id,
                        InterestId = interest.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _context.UserInterests.AddAsync(userInterest, cancellationToken);
                }

                hasChanges = true;
            }
        }

        // Сохраняем изменения, если они есть
        if (hasChanges)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Загружаем обновленного пользователя с интересами для DTO
        var updatedUser = await _context.Users
            .Include(u => u.UserInterests)
            .ThenInclude(ui => ui.Interest)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        return updatedUser != null ? MapToDto(updatedUser) : null;
    }

    private static UserDto MapToDto(User user)
    {
        // Получаем имена интересов из UserInterests
        var interests = user.UserInterests?
            .Where(ui => ui.Interest != null)
            .Select(ui => ui.Interest.Name)
            .ToList() ?? new List<string>();

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
