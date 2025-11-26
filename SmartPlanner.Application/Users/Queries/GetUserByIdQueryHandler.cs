// SmartPlanner.Application/Users/Queries/GetUserByIdQueryHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Queries;

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            return user != null ? MapToDto(user) : null;
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
