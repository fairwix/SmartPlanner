// SmartPlanner.Application/Users/Commands/CreateUserCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Commands
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;

        public CreateUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // Check if user already exists
            if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
                throw new ArgumentException($"User with email {request.Email} already exists");

            if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
                throw new ArgumentException($"User with username {request.Username} already exists");

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

            // Save to repository
            var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

            // Map to DTO
            return MapToDto(createdUser);
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Interests = user.Interests,
                Balance = user.Balance,
                StreakCount = user.StreakCount,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}