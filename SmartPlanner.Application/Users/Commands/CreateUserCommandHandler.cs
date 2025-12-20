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
                Balance = 0,
                StreakCount = 0,
                LastLoginAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user, cancellationToken);
            if (request.Interests?.Any() == true)
            {
                foreach (var interestName in request.Interests)
                {
                    var interest = await _context.Interests
                        .FirstOrDefaultAsync(i => i.Name == interestName, cancellationToken);

                    if (interest == null)
                    {
                        interest = new Interest
                        {
                            Name = interestName,
                            Description = null
                        };
                        await _context.Interests.AddAsync(interest, cancellationToken);
                    }

                    var userInterest = new UserInterest
                    {
                        UserId = user.Id,
                        InterestId = interest.Id
                    };
                    await _context.UserInterests.AddAsync(userInterest, cancellationToken);
                }
            }
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
                user.UserInterests?.Select(ui => ui.Interest.Name).ToList() ?? new List<string>(),
                user.Balance,
                user.StreakCount,
                user.LastLoginAt);
        }
    }
