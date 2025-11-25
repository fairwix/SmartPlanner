// SmartPlanner.Application/Users/Commands/AddFriendCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Users.Commands
{
    public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, bool>
    {
        private readonly IUserRepository _userRepository;

        public AddFriendCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> Handle(AddFriendCommand request, CancellationToken cancellationToken)
        {
            // Check if both users exist
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            var friend = await _userRepository.GetByIdAsync(request.FriendId, cancellationToken);

            if (user == null || friend == null)
                return false;

            // Check if not already friends
            var existingFriends = await _userRepository.GetUserFriendsAsync(request.UserId, cancellationToken);
            if (existingFriends.Any(f => f.Id == request.FriendId))
                return false;

            return await _userRepository.AddFriendAsync(request.UserId, request.FriendId, cancellationToken);
        }
    }
}