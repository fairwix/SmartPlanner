// SmartPlanner.Application/Users/Commands/CreateUserCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Commands;

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
                return MapToDto(await _userRepository.GetByEmailAsync(request.Email, cancellationToken));

            if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
                return MapToDto(await _userRepository.GetByUsernameAsync(request.Username, cancellationToken));

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
