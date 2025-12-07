using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.DTOs.User;
using Microsoft.Extensions.Logging;

namespace SmartPlanner.Application.Interfaces.Services;
#if false
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Создание пользователя: {Username}", request.Username);

            try
            {
                if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
                {
                    _logger.LogWarning("Попытка создания пользователя с существующим email: {Email}", request.Email);
                    throw new ArgumentException(nameof(request.Email), "Пользователь с таким email уже существует");
                }

                if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
                {
                    _logger.LogWarning("Попытка создания пользователя с существующим именем: {Username}", request.Username);
                    throw new ArgumentException(nameof(request.Username), "Пользователь с таким именем уже существует");
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Interests = request.Interests ?? new List<string>(),
                    Balance = 0,
                    LastLogin = DateTime.UtcNow,
                    StreakCount = 0
                };

                _logger.LogDebug("Создание пользователя в репозитории: {@User}", user);

                var result = await _userRepository.CreateAsync(user, cancellationToken);

                _logger.LogInformation("Пользователь успешно создан с ID: {UserId}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании пользователя {Username}", request.Username);
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Получение пользователя по ID: {UserId}", id);

            try
            {
                var user = await _userRepository.GetByIdAsync(id, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с ID {UserId} не найден", id);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", id);
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Получение пользователя по email: {Email}", email);

            try
            {
                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с email {Email} не найден", email);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя по email {Email}", email);
                throw;
            }
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Поиск пользователей по запросу: {SearchTerm}", searchTerm);

            try
            {
                var allUsers = await _userRepository.GetAllAsync(cancellationToken);
                var result = allUsers.Where(u =>
                    u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                _logger.LogInformation("Найдено {Count} пользователей по запросу '{SearchTerm}'", result.Count, searchTerm);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске пользователей по запросу '{SearchTerm}'", searchTerm);
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Обновление пользователя {UserId}", id);

            try
            {
                var user = await _userRepository.GetByIdAsync(id, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для обновления", id);
                    throw new ArgumentException(nameof(user), "Пользователь не найден");
                }

                if (request.Interests != null)
                {
                    user.Interests = request.Interests;
                }
                user.UpdatedAt = DateTime.UtcNow;

                _logger.LogDebug("Обновление пользователя в репозитории: {@User}", user);

                var result = await _userRepository.UpdateAsync(user, cancellationToken) ?? user;

                _logger.LogInformation("Пользователь {UserId} успешно обновлен", id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Удаление пользователя {UserId}", id);

            try
            {
                var result = await _userRepository.DeleteAsync(id, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Пользователь {UserId} успешно удален", id);
                }
                else
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для удаления", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
                throw;
            }
        }

        public async Task<bool> AddUserInterestAsync(Guid userId, string interest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Добавление интереса '{Interest}' пользователю {UserId}", interest, userId);

            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для добавления интереса", userId);
                    return false;
                }

                if (!user.Interests.Contains(interest))
                {
                    user.Interests.Add(interest);
                    await _userRepository.UpdateAsync(user, cancellationToken);
                    _logger.LogInformation("Интерес '{Interest}' добавлен пользователю {UserId}", interest, userId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Интерес '{Interest}' уже существует у пользователя {UserId}", interest, userId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении интереса '{Interest}' пользователю {UserId}", interest, userId);
                throw;
            }
        }

        public async Task<bool> RemoveUserInterestAsync(Guid userId, string interest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Удаление интереса '{Interest}' у пользователя {UserId}", interest, userId);

            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для удаления интереса", userId);
                    return false;
                }

                var removed = user.Interests.Remove(interest);

                if (removed)
                {
                    await _userRepository.UpdateAsync(user, cancellationToken);
                    _logger.LogInformation("Интерес '{Interest}' удален у пользователя {UserId}", interest, userId);
                }
                else
                {
                    _logger.LogWarning("Интерес '{Interest}' не найден у пользователя {UserId}", interest, userId);
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении интереса '{Interest}' у пользователя {UserId}", interest, userId);
                throw;
            }
        }

        public async Task<bool> AddFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Добавление друга {FriendId} пользователю {UserId}", friendId, userId);

            try
            {
                var result = await _userRepository.AddFriendAsync(userId, friendId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Друг {FriendId} успешно добавлен пользователю {UserId}", friendId, userId);
                }
                else
                {
                    _logger.LogWarning("Не удалось добавить друга {FriendId} пользователю {UserId}", friendId, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении друга {FriendId} пользователю {UserId}", friendId, userId);
                throw;
            }
        }

        public async Task<bool> RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Удаление друга {FriendId} у пользователя {UserId}", friendId, userId);

            try
            {
                var result = await _userRepository.RemoveFriendAsync(userId, friendId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Друг {FriendId} успешно удален у пользователя {UserId}", friendId, userId);
                }
                else
                {
                    _logger.LogWarning("Не удалось удалить друга {FriendId} у пользователя {UserId}", friendId, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении друга {FriendId} у пользователя {UserId}", friendId, userId);
                throw;
            }
        }

        public async Task<List<User>> GetUserFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение списка друзей пользователя {UserId}", userId);

            try
            {
                var friends = await _userRepository.GetUserFriendsAsync(userId, cancellationToken);
                _logger.LogInformation("Найдено {Count} друзей пользователя {UserId}", friends.Count, userId);
                return friends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении друзей пользователя {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUserBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Получение баланса пользователя {UserId}", userId);

            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                var balance = user?.Balance ?? 0;

                _logger.LogDebug("Баланс пользователя {UserId}: {Balance}", userId, balance);
                return balance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении баланса пользователя {UserId}", userId);
                throw;
            }
        }

        public async Task<User> AddRewardToUserAsync(Guid userId, int amount, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Добавление награды {Amount} пользователю {UserId}", amount, userId);

            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для добавления награды", userId);
                    throw new ArgumentException(nameof(user), "Пользователь не найден");
                }

                user.AddReward(amount);

                _logger.LogDebug("Обновление пользователя с новой наградой: {@User}", user);

                var result = await _userRepository.UpdateAsync(user, cancellationToken) ?? user;

                _logger.LogInformation("Награда {Amount} успешно добавлена пользователю {UserId}. Новый баланс: {Balance}",
                    amount, userId, result.Balance);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении награды {Amount} пользователю {UserId}", amount, userId);
                throw;
            }
        }
    }
#endif
