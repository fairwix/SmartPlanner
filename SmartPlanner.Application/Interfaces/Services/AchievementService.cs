using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Interfaces.Services;

    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _achievementRepository;
        private readonly IUserAchievementRepository _userAchievementRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            IAchievementRepository achievementRepository,
            IUserAchievementRepository userAchievementRepository,
            IUserRepository userRepository,
            ILogger<AchievementService> logger)
        {
            _achievementRepository = achievementRepository;
            _userAchievementRepository = userAchievementRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<Achievement>> GetAchievementsByTypeAsync(AchievementType type, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение достижений типа {Type}", type);

            try
            {
                var achievements = await _achievementRepository.GetAchievementsByTypeAsync(type, cancellationToken);
                _logger.LogInformation("Найдено {Count} достижений типа {Type}", achievements.Count, type);
                return achievements;
            }
            catch (Exception ex) // никогда не стоит просто поймать все исключения
            {
                _logger.LogError(ex, "Ошибка при получении достижений типа {Type}", type);
                throw;
            }
        }

        public async Task<Achievement?> GetAchievementByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Получение достижения по ID: {AchievementId}", id);

            try
            {
                var achievement = await _achievementRepository.GetByIdAsync(id, cancellationToken);

                if (achievement == null)
                {
                    _logger.LogWarning("Достижение с ID {AchievementId} не найдено", id);
                }

                return achievement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении достижения {AchievementId}", id);
                throw;
            }
        }

        public async Task<List<Achievement>> GetAllAchievementsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение всех достижений");

            try
            {
                var achievements = await _achievementRepository.GetAllAsync(cancellationToken);
                _logger.LogInformation("Найдено {Count} достижений", achievements.Count);
                return achievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всех достижений");
                throw;
            }
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение достижений пользователя {UserId}", userId);

            try
            {
                var userAchievements = await _userAchievementRepository.GetByUserIdAsync(userId, cancellationToken);
                _logger.LogInformation("Найдено {Count} достижений пользователя {UserId}", userAchievements.Count, userId);
                return userAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении достижений пользователя {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AwardAchievementToUserAsync(Guid userId, Guid achievementId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Выдача достижения {AchievementId} пользователю {UserId}", achievementId, userId);

            try
            {
                // Проверяем существование пользователя
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для выдачи достижения", userId);
                    throw new ArgumentException(nameof(userId), $"Пользователь с ID {userId} не найден");
                }

                // Проверяем существование достижения
                var achievement = await _achievementRepository.GetByIdAsync(achievementId, cancellationToken);
                if (achievement == null)
                {
                    _logger.LogWarning("Достижение {AchievementId} не найдено для выдачи пользователю {UserId}", achievementId, userId);
                    throw new ArgumentException(nameof(achievementId), $"Достижение с ID {achievementId} не найдено");
                }

                // Проверяем, не получено ли уже достижение
                if (await _userAchievementRepository.ExistsAsync(userId, achievementId, cancellationToken))
                {
                    _logger.LogWarning("Достижение {AchievementId} уже выдано пользователю {UserId}", achievementId, userId);
                    return false;
                }

                var userAchievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievementId,
                    AwardedAt = DateTime.UtcNow
                };

                _logger.LogDebug("Создание записи о достижении пользователя: {@UserAchievement}", userAchievement);

                await _userAchievementRepository.CreateAsync(userAchievement, cancellationToken);

                // Награждаем пользователя
                user.AddReward(achievement.RewardAmount);
                await _userRepository.UpdateAsync(user, cancellationToken);

                _logger.LogInformation("Достижение '{AchievementName}' успешно выдано пользователю {UserId}. Начислено {Reward} очков",
                    achievement.Name, userId, achievement.RewardAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выдаче достижения {AchievementId} пользователю {UserId}", achievementId, userId);
                throw;
            }
        }

        public async Task CheckAndAwardAchievementsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Проверка и выдача достижений пользователю {UserId}", userId);

            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для проверки достижений", userId);
                    return;
                }

                var eligibleAchievements = await GetEligibleAchievementsForUserAsync(userId, cancellationToken);
                _logger.LogDebug("Найдено {Count} достижений, доступных для выдачи пользователю {UserId}",
                    eligibleAchievements.Count, userId);

                var awardedCount = 0;
                foreach (var achievement in eligibleAchievements)
                {
                    if (achievement.CanBeAwarded(user))
                    {
                        await AwardAchievementToUserAsync(userId, achievement.Id, cancellationToken);
                        awardedCount++;
                        _logger.LogDebug("Достижение '{AchievementName}' выдано пользователю {UserId}", achievement.Name, userId);
                    }
                }

                _logger.LogInformation("Проверка достижений завершена. Выдано {AwardedCount} достижений пользователю {UserId}",
                    awardedCount, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке и выдаче достижений пользователю {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Achievement>> GetEligibleAchievementsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Получение доступных достижений для пользователя {UserId}", userId);

            try
            {
                var allAchievements = await _achievementRepository.GetAllAsync(cancellationToken);
                var userAchievements = await _userAchievementRepository.GetByUserIdAsync(userId, cancellationToken);

                var awardedAchievementIds = userAchievements.Select(ua => ua.AchievementId).ToHashSet();

                var eligible = allAchievements
                    .Where(a => !awardedAchievementIds.Contains(a.Id))
                    .ToList();

                _logger.LogDebug("Найдено {Count} доступных достижений для пользователя {UserId}", eligible.Count, userId);
                return eligible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении доступных достижений для пользователя {UserId}", userId);
                throw;
            }
        }
    }

