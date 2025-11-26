using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Common.Interfaces.Repositories;

    public interface IUserRepository
    {
        // Базовые операции
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default);
        Task<User?> UpdateAsync(User entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        // Специфичные методы
        Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<List<User>> GetUserFriendsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> AddFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
        Task<bool> RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
    }
