using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;

using SmartPlanner.Application.DTOs.Goal;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Interfaces.Services;

    public class GoalService : IGoalService
    {
        private readonly IGoalRepository _goalRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GoalService> _logger;

        public GoalService(IGoalRepository goalRepository, IUserRepository userRepository, ILogger<GoalService> logger)
        {
            _goalRepository = goalRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Goal> CreateGoalAsync(CreateGoalRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Создание цели для пользователя {UserId}: {Title}", request.UserId, request.Title);

            try
            {
                // Проверяем существование пользователя
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден при создании цели", request.UserId);
                    throw new ArgumentException(nameof(request.UserId), $"Пользователь с ID {request.UserId} не найден");
                }

                var goal = new Goal
                {
                    Title = request.Title.Trim(),
                    Description = request.Description,
                    Category = request.Category,
                    Priority = request.Priority,
                    DueDate = request.DueDate,
                    TargetValue = request.TargetValue,
                    UserId = request.UserId,
                    RewardAmount = 10
                };

                if (!goal.IsValid())
                {
                    _logger.LogWarning("Некорректные данные цели: {@Goal}", goal);
                    throw new ArgumentException(nameof(goal.IsValid), "Некорректные данные цели");
                }

                _logger.LogDebug("Создание цели в репозитории: {@Goal}", goal);

                var createdGoal = await _goalRepository.CreateAsync(goal, cancellationToken);

                // Начисляем награду за создание цели
                user.AddReward(5);
                await _userRepository.UpdateAsync(user, cancellationToken);

                _logger.LogInformation("Цель успешно создана с ID: {GoalId}. Пользователь получил +5 очков", createdGoal.Id);

                return createdGoal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании цели для пользователя {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<Goal> UpdateGoalProgressAsync(Guid goalId, int progressValue, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Обновление прогресса цели {GoalId} на {Progress}", goalId, progressValue);

            try
            {
                var goal = await _goalRepository.GetByIdAsync(goalId, cancellationToken);
                if (goal == null)
                {
                    _logger.LogWarning("Цель {GoalId} не найдена для обновления прогресса", goalId);
                    throw new ArgumentException(nameof(goal), "Цель не найдена");
                }

                goal.UpdateProgress(progressValue);

                _logger.LogDebug("Прогресс цели {GoalId} обновлен: {CurrentValue}/{TargetValue}",
                    goalId, goal.CurrentValue, goal.TargetValue);

                var updatedGoal = await _goalRepository.UpdateAsync(goal, cancellationToken) ?? goal;

                if (goal.IsCompleted)
                {
                    _logger.LogInformation("Цель {GoalId} завершена! Начислено {Reward} очков", goalId, goal.RewardAmount);
                }

                return updatedGoal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении прогресса цели {GoalId}", goalId);
                throw;
            }
        }

        public async Task<Goal> UpdateGoalAsync(Goal goal, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Обновление цели {GoalId}", goal.Id);

            try
            {
                goal.UpdatedAt = DateTime.UtcNow;

                _logger.LogDebug("Обновление цели в репозитории: {@Goal}", goal);

                var updatedGoal = await _goalRepository.UpdateAsync(goal, cancellationToken) ?? goal;

                _logger.LogInformation("Цель {GoalId} успешно обновлена", goal.Id);
                return updatedGoal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении цели {GoalId}", goal.Id);
                throw;
            }
        }

        public async Task<bool> DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Удаление цели {GoalId}", goalId);

            try
            {
                var result = await _goalRepository.DeleteAsync(goalId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Цель {GoalId} успешно удалена", goalId);
                }
                else
                {
                    _logger.LogWarning("Цель {GoalId} не найдена для удаления", goalId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении цели {GoalId}", goalId);
                throw;
            }
        }

        public async Task<Goal> GetGoalByIdAsync(Guid goalId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Получение цели по ID: {GoalId}", goalId);

            try
            {
                var goal = await _goalRepository.GetByIdAsync(goalId, cancellationToken);

                if (goal == null)
                {
                    _logger.LogWarning("Цель с ID {GoalId} не найдена", goalId);
                }

                return goal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении цели {GoalId}", goalId);
                throw;
            }
        }

        public async Task<List<Goal>> GetUserGoalsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение целей пользователя {UserId}", userId);

            try
            {
                var goals = await _goalRepository.GetUserGoalsAsync(userId, cancellationToken);

                var completed = goals.Count(g => g.IsCompleted);
                var active = goals.Count - completed;

                _logger.LogInformation("Найдено {Total} целей пользователя {UserId} ({Active} активных, {Completed} завершенных)",
                    goals.Count, userId, active, completed);

                return goals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении целей пользователя {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Goal>> GetUserGoalsPagedAsync(Guid userId, int pageNumber, int pageSize, string sortBy = "CreatedAt", string sortOrder = "desc", CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение страницы {PageNumber} целей пользователя {UserId} (размер: {PageSize})",
                pageNumber, userId, pageSize);

            try
            {
                var userGoals = await GetUserGoalsAsync(userId, cancellationToken);

                _logger.LogDebug("Сортировка целей по {SortBy} в порядке {SortOrder}", sortBy, sortOrder);

                var sortedGoals = sortBy.ToLower() switch
                {
                    "title" => sortOrder.ToLower() == "desc" ?
                        userGoals.OrderByDescending(g => g.Title) :
                        userGoals.OrderBy(g => g.Title),
                    "duedate" => sortOrder.ToLower() == "desc" ?
                        userGoals.OrderByDescending(g => g.DueDate) :
                        userGoals.OrderBy(g => g.DueDate),
                    "priority" => sortOrder.ToLower() == "desc" ?
                        userGoals.OrderByDescending(g => g.Priority) :
                        userGoals.OrderBy(g => g.Priority),
                    "progresspercentage" => sortOrder.ToLower() == "desc" ?
                        userGoals.OrderByDescending(g => g.ProgressPercentage) :
                        userGoals.OrderBy(g => g.ProgressPercentage),
                    _ => sortOrder.ToLower() == "desc" ?
                        userGoals.OrderByDescending(g => g.CreatedAt) :
                        userGoals.OrderBy(g => g.CreatedAt)
                };

                var totalCount = userGoals.Count;
                var pagedGoals = sortedGoals
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Возвращено {Count} целей на странице {PageNumber} из {TotalPages}",
                    pagedGoals.Count, pageNumber, (int)Math.Ceiling(totalCount / (double)pageSize));

                return pagedGoals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении страницы целей пользователя {UserId}", userId);
                throw;
            }
        }
    }

