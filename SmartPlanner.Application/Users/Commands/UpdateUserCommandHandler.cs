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
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return null;

        // Проверяем уникальность username если он изменяется
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username && u.Id != request.UserId,
                         cancellationToken);

            if (usernameExists)
                throw new ArgumentException($"Username '{request.Username}' is already taken");

            // EF Core не позволяет менять init-поля, создаем новый объект
            var updatedUser = new User
            {
                Id = user.Id,
                Username = request.Username,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                Interests = request.Interests ?? user.Interests,
                Balance = user.Balance,
                StreakCount = user.StreakCount,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            // Удаляем старый и добавляем новый
            _context.Users.Remove(user);
            await _context.Users.AddAsync(updatedUser, cancellationToken);
        }
        else if (request.Interests is not null)
        {
            // Только интересы меняем
            user.Interests = request.Interests;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Получаем обновленного пользователя
        var finalUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        return finalUser != null ? MapToDto(finalUser) : null;
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.CreatedAt,
        user.UpdatedAt,
        user.Username,
        user.Email,
        user.Interests,
        user.Balance,
        user.StreakCount,
        user.LastLogin);
}
