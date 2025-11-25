using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.DTOs.Challenge;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Domain.Interfaces.Services
{
    public class ChallengeService : IChallengeService
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ChallengeService> _logger;

        public ChallengeService(
            IChallengeRepository challengeRepository, 
            IUserRepository userRepository,
            ILogger<ChallengeService> logger)
        {
            _challengeRepository = challengeRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Challenge> CreateChallengeAsync(CreateChallengeRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Начало создания челленджа: {Title}", request.Title);
            
            try
            {
                // Проверяем существование пользователя
                var user = await _userRepository.GetByIdAsync(request.CreatedBy, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден при создании челленджа", request.CreatedBy);
                    throw new ArgumentException($"Пользователь с ID {request.CreatedBy} не найден");
                }

                var challenge = new Challenge
                {
                    Title = request.Title,
                    Description = request.Description,
                    Type = request.Type,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsGroupChallenge = request.IsGroupChallenge,
                    TargetValue = request.TargetValue,
                    CurrentValue = 0,
                    CreatedBy = request.CreatedBy
                };

                _logger.LogDebug("Создание челленджа в репозитории: {@Challenge}", challenge);
                
                var result = await _challengeRepository.CreateAsync(challenge, cancellationToken);
                
                _logger.LogInformation("Челлендж успешно создан с ID: {ChallengeId}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании челленджа: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<Challenge?> GetChallengeByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Получение челленджа по ID: {ChallengeId}", id);
            
            try
            {
                var challenge = await _challengeRepository.GetByIdAsync(id, cancellationToken);
                
                if (challenge == null)
                {
                    _logger.LogWarning("Челлендж с ID {ChallengeId} не найден", id);
                }
                else
                {
                    _logger.LogDebug("Челлендж найден: {ChallengeTitle}", challenge.Title);
                }
                
                return challenge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении челленджа {ChallengeId}", id);
                throw;
            }
        }

        public async Task<List<Challenge>> GetActiveChallengesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение списка активных челленджей");
            
            try
            {
                var challenges = await _challengeRepository.GetActiveChallengesAsync(cancellationToken);
                _logger.LogInformation("Найдено {Count} активных челленджей", challenges.Count);
                return challenges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении активных челленджей");
                throw;
            }
        }

        public async Task<List<Challenge>> GetUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Получение челленджей пользователя {UserId}", userId);
            
            try
            {
                var challenges = await _challengeRepository.GetUserChallengesAsync(userId, cancellationToken);
                _logger.LogInformation("Найдено {Count} челленджей для пользователя {UserId}", challenges.Count, userId);
                return challenges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении челленджей пользователя {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> JoinChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Пользователь {UserId} присоединяется к челленджу {ChallengeId}", userId, challengeId);
            
            try
            {
                // Проверяем существование пользователя
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден при присоединении к челленджу", userId);
                    throw new ArgumentException($"Пользователь с ID {userId} не найден");
                }

                // Проверяем существование челленджа
                var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
                if (challenge == null)
                {
                    _logger.LogWarning("Челлендж {ChallengeId} не найден при присоединении пользователя {UserId}", challengeId, userId);
                    throw new ArgumentException($"Челлендж с ID {challengeId} не найден");
                }

                if (!challenge.IsActive)
                {
                    _logger.LogWarning("Челлендж {ChallengeId} не активен. Пользователь {UserId} не может присоединиться", challengeId, userId);
                    throw new ArgumentException("Челлендж не активен");
                }

                var result = await _challengeRepository.AddParticipantToChallengeAsync(challengeId, userId, cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("Пользователь {UserId} успешно присоединился к челленджу {ChallengeId}", userId, challengeId);
                }
                else
                {
                    _logger.LogWarning("Не удалось присоединить пользователя {UserId} к челленджу {ChallengeId}", userId, challengeId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при присоединении пользователя {UserId} к челленджу {ChallengeId}", userId, challengeId);
                throw;
            }
        }

        public async Task<bool> LeaveChallengeAsync(Guid challengeId, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Пользователь {UserId} покидает челлендж {ChallengeId}", userId, challengeId);
            
            try
            {
                var result = await _challengeRepository.RemoveParticipantFromChallengeAsync(challengeId, userId, cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("Пользователь {UserId} успешно покинул челлендж {ChallengeId}", userId, challengeId);
                }
                else
                {
                    _logger.LogWarning("Не удалось удалить пользователя {UserId} из челленджа {ChallengeId}", userId, challengeId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе пользователя {UserId} из челленджа {ChallengeId}", userId, challengeId);
                throw;
            }
        }

        public async Task<Challenge> UpdateChallengeProgressAsync(Guid challengeId, int progress, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Обновление прогресса челленджа {ChallengeId} на {Progress}", challengeId, progress);
            
            try
            {
                var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
                if (challenge == null)
                {
                    _logger.LogWarning("Челлендж {ChallengeId} не найден для обновления прогресса", challengeId);
                    throw new ArgumentException("Челлендж не найден");
                }

                if (!challenge.IsActive)
                {
                    _logger.LogWarning("Челлендж {ChallengeId} не активен, обновление прогресса невозможно", challengeId);
                    throw new ArgumentException("Челлендж не активен");
                }

                challenge.CurrentValue = Math.Min(progress, challenge.TargetValue);
                var updatedChallenge = await _challengeRepository.UpdateAsync(challenge, cancellationToken) ?? challenge;
                
                _logger.LogInformation("Прогресс челленджа {ChallengeId} успешно обновлен на {CurrentValue}/{TargetValue}", 
                    challengeId, updatedChallenge.CurrentValue, updatedChallenge.TargetValue);
                
                return updatedChallenge;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении прогресса челленджа {ChallengeId}", challengeId);
                throw;
            }
        }

        public async Task<List<Challenge>> GenerateAiChallengesAsync(Guid userId, int count = 3, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Генерация {Count} AI-челленджей для пользователя {UserId}", count, userId);
            
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден для генерации AI-челленджей", userId);
                    return new List<Challenge>();
                }

                _logger.LogDebug("Интересы пользователя для генерации: {@Interests}", user.Interests);

                var aiChallenges = new List<Challenge>();
                var random = new Random();

                foreach (var interest in user.Interests.Take(count))
                {
                    var challenge = new Challenge
                    {
                        Title = GenerateChallengeTitle(interest),
                        Description = GenerateChallengeDescription(interest),
                        Type = GetChallengeTypeFromInterest(interest),
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(7),
                        IsGroupChallenge = random.Next(0, 2) == 1,
                        TargetValue = GenerateTargetValue(interest),
                        CurrentValue = 0,
                        CreatedBy = userId
                    };

                    aiChallenges.Add(challenge);
                    _logger.LogDebug("Сгенерирован AI-челлендж: {Title}", challenge.Title);
                }

                _logger.LogInformation("Успешно сгенерировано {Count} AI-челленджей для пользователя {UserId}", aiChallenges.Count, userId);
                return aiChallenges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации AI-челленджей для пользователя {UserId}", userId);
                throw;
            }
        }

        private string GenerateChallengeTitle(string interest)
        {
            var title = interest.ToLower() switch
            {
                "sports" => "7-Day Fitness Challenge",
                "reading" => "Book Reading Marathon",
                "programming" => "Code Every Day Challenge",
                "music" => "Daily Practice Challenge",
                _ => $"{interest} Master Challenge"
            };
            
            _logger.LogDebug("Сгенерирован заголовок '{Title}' для интереса '{Interest}'", title, interest);
            return title;
        }

        private string GenerateChallengeDescription(string interest)
        {
            return $"AI-generated challenge based on your interest in {interest}. Complete this challenge to earn rewards!";
        }

        private ChallengeType GetChallengeTypeFromInterest(string interest)
        {
            var type = interest.ToLower() switch
            {
                "sports" or "fitness" => ChallengeType.Exercise,
                "reading" or "books" => ChallengeType.Reading,
                "programming" or "coding" => ChallengeType.Learning,
                _ => ChallengeType.Custom
            };
            
            _logger.LogDebug("Сгенерирован тип '{Type}' для интереса '{Interest}'", type, interest);
            return type;
        }

        private int GenerateTargetValue(string interest)
        {
            var random = new Random();
            var target = interest.ToLower() switch
            {
                "sports" => random.Next(5000, 20000),
                "reading" => random.Next(1, 5),
                "programming" => random.Next(7, 30),
                _ => random.Next(5, 20)
            };
            
            _logger.LogDebug("Сгенерировано целевое значение '{Target}' для интереса '{Interest}'", target, interest);
            return target;
        }
    }
}