// SmartPlanner.Application/Users/Commands/CreateUserCommandHandler.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Users.Commands;

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IApplicationDbContext _context;

        public CreateUserCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // Check if user already exists by email
            var existingByEmail = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (existingByEmail != null)
                return MapToDto(existingByEmail);

            // Check if user already exists by username
            var existingByUsername = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (existingByUsername != null)
                return MapToDto(existingByUsername);

            // Create user entity
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Interests = request.Interests ?? new List<string>(),
                Balance = 0,
                StreakCount = 0,
                LastLogin = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(user);
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto(
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
    }
