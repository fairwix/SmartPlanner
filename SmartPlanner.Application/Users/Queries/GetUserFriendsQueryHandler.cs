// SmartPlanner.Application/Users/Queries/GetUserFriendsQueryHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Queries
{
    public class GetUserFriendsQueryHandler : IRequestHandler<GetUserFriendsQuery, List<UserDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetUserFriendsQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserDto>> Handle(GetUserFriendsQuery request, CancellationToken cancellationToken)
        {
            var friends = await _userRepository.GetUserFriendsAsync(request.UserId, cancellationToken);
            return friends.Select(MapToDto).ToList();
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