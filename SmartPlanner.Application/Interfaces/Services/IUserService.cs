using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.DTOs.User;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Interfaces.Services;

    public interface IUserService
    {
        Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<List<User>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<User> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> AddUserInterestAsync(Guid userId, string interest, CancellationToken cancellationToken = default);
        Task<bool> RemoveUserInterestAsync(Guid userId, string interest, CancellationToken cancellationToken = default);
        Task<bool> AddFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
        Task<bool> RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
        Task<List<User>> GetUserFriendsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetUserBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<User> AddRewardToUserAsync(Guid userId, int amount, CancellationToken cancellationToken = default);
    }

