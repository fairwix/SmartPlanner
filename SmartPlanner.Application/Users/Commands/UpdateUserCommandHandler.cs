// SmartPlanner.Application/Users/Commands/UpdateUserCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;

using SmartPlanner.Application.Users.Dtos;


namespace SmartPlanner.Application.Users.Commands
{
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

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Username))
                user.Username = request.Username;

            if (request.Interests != null)
                user.Interests = request.Interests;

            user.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);
            return updatedUser != null ? MapToDto(updatedUser) : null;
        }

        private UserDto MapToDto(Domain.Entities.User user)
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