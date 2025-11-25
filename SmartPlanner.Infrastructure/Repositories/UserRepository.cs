using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Infrastructure.Repositories
{
    public class UserRepository : FileStorageRepository<User>, IUserRepository
    {
        public UserRepository(string filePath) : base(filePath) { }

        // Базовые методы из интерфейса
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.GetByIdAsync(id, cancellationToken);

        public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
            => await base.GetAllAsync(cancellationToken);

        public async Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default)
            => await base.CreateAsync(entity, cancellationToken);

        public async Task<User?> UpdateAsync(User entity, CancellationToken cancellationToken = default)
            => await base.UpdateAsync(entity, cancellationToken);

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => await base.DeleteAsync(id, cancellationToken);

        // Специфичные методы (без изменений)
        public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var users = await base.GetAllAsync(cancellationToken);
            return users.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var users = await base.GetAllAsync(cancellationToken);
            return users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await FindByEmailAsync(email, cancellationToken);
            return user != null;
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var user = await FindByUsernameAsync(username, cancellationToken);
            return user != null;
        }

        public async Task<List<User>> GetUserFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var users = await base.GetAllAsync(cancellationToken);
            var user = await GetByIdAsync(userId, cancellationToken);
            
            if (user == null) return new List<User>();

            var friendIds = user.Friends
                .Where(f => f.Status == FriendStatus.Accepted)
                .Select(f => f.FriendId)
                .ToList();

            return users.Where(u => friendIds.Contains(u.Id)).ToList();
        }

        public async Task<bool> AddFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            var users = await base.GetAllAsync(cancellationToken);
            var user = await GetByIdAsync(userId, cancellationToken);
            var friend = await GetByIdAsync(friendId, cancellationToken);

            if (user == null || friend == null) return false;

            if (user.Friends.Any(f => f.FriendId == friendId))
                return false;

            user.Friends.Add(new UserFriend
            {
                UserId = userId,
                FriendId = friendId,
                Status = FriendStatus.Pending
            });

            await UpdateAsync(user, cancellationToken);
            return true;
        }

        public async Task<bool> RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            var friend = user.Friends.FirstOrDefault(f => f.FriendId == friendId);
            if (friend == null) return false;

            user.Friends.Remove(friend);
            await UpdateAsync(user, cancellationToken);
            return true;
        }
    }
}