// SmartPlanner.Application/Users/Queries/GetUserFriendsQueryHandler.cs
using MediatR;
using SmartPlanner.Application.Users.Dtos;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Queries;

    public class GetUserFriendsQueryHandler : IRequestHandler<GetUserFriendsQuery, List<UserDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetUserFriendsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserDto>> Handle(GetUserFriendsQuery request, CancellationToken cancellationToken)
        {
            var friends = await _context.UserFriends
                .AsNoTracking()
                .Include(uf => uf.Friend)
                .Where(uf => uf.UserId == request.UserId && uf.Status == FriendStatus.Accepted)
                .Select(uf => uf.Friend)
                .ToListAsync(cancellationToken);

            return friends.Select(MapToDto).ToList();
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
