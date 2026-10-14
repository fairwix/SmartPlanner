using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Commands;

    public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public AddFriendCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(AddFriendCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            var friend = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.FriendId, cancellationToken);

            if (user == null || friend == null)
                return false;

            var alreadyFriends = await _context.UserFriends
                .AsNoTracking()
                .AnyAsync(uf => uf.UserId == request.UserId && uf.FriendId == request.FriendId, cancellationToken);

            if (alreadyFriends)
                return false;

            var userFriend = new UserFriend
            {
                UserId = request.UserId,
                FriendId = request.FriendId,
                Status = FriendStatus.Pending
            };

            await _context.UserFriends.AddAsync(userFriend, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
