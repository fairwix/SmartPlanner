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
        var user = await _context.Users
            .Include(u => u.UserInterests)
            .ThenInclude(ui => ui.Interest)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return null;

        bool hasChanges = false;

        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username && u.Id != request.UserId,
                         cancellationToken);

            if (usernameExists)
                throw new ArgumentException($"Username '{request.Username}' is already taken");

            user.Username = request.Username;
            hasChanges = true;
        }

        if (request.Interests is not null)
        {
            var currentInterestNames = user.UserInterests
                .Select(ui => ui.Interest.Name)
                .ToList();

            var newInterestNames = request.Interests
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct()
                .ToList();

            if (!currentInterestNames.SequenceEqual(newInterestNames))
            {
                var existingUserInterests = _context.UserInterests
                    .Where(ui => ui.UserId == request.UserId);
                _context.UserInterests.RemoveRange(existingUserInterests);

                foreach (var interestName in newInterestNames)
                {
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

                        await _context.SaveChangesAsync(cancellationToken);
                    }

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
