// SmartPlanner.Application/Users/Commands/UpdateUserCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;

using SmartPlanner.Application.Users.Dtos;


namespace SmartPlanner.Application.Users.Commands;

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto?>
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return null;

            // Create a new instance with updated values
            var updatedUser = new Domain.Entities.User
            {
                Id = user.Id,
                CreatedAt = user.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                Username = !string.IsNullOrEmpty(request.Username) ? request.Username : user.Username,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                Balance = user.Balance,
                Interests = request.Interests ?? user.Interests,
                LastLogin = user.LastLogin,
                StreakCount = user.StreakCount,
                Goals = user.Goals,
                Friends = user.Friends,
                Achievements = user.Achievements,
                CreatedChallenges = user.CreatedChallenges,
                ChallengeParticipants = user.ChallengeParticipants
            };

            var result = await _userRepository.UpdateAsync(updatedUser, cancellationToken);
            return result != null ? MapToDto(result) : null;
        }

        private UserDto MapToDto(Domain.Entities.User user)
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

