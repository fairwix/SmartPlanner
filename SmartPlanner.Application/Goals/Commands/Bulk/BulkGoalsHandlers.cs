// SmartPlanner.Application/Goals/Commands/BulkGoalsHandlers.cs

using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Goals.Commands
{
    public class BulkCreateGoalsCommandHandler : IRequestHandler<BulkCreateGoalsCommand, BulkOperationResult<GoalDto>>
    {
        private readonly IGoalRepository _goalRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<BulkCreateGoalsCommandHandler> _logger;

        public BulkCreateGoalsCommandHandler(
            IGoalRepository goalRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<BulkCreateGoalsCommandHandler> logger)
        {
            _goalRepository = goalRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BulkOperationResult<GoalDto>> Handle(
            BulkCreateGoalsCommand request, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting bulk creation of {GoalCount} goals for user {UserId}", 
                request.Goals.Count, request.UserId);

            var results = new BulkOperationResult<GoalDto>
            {
                TotalCount = request.Goals.Count
            };

            // Проверяем существование пользователя
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                var errorMessage = $"User with ID {request.UserId} not found";
                _logger.LogWarning(errorMessage);
                
                // Все операции провалятся из-за отсутствия пользователя
                foreach (var goalDto in request.Goals)
                {
                    results.Items.Add(new BulkOperationItem<GoalDto>
                    {
                        Success = false,
                        Message = errorMessage,
                        Error = errorMessage
                    });
                }
                results.FailedCount = request.Goals.Count;
                return results;
            }

            // Обрабатываем каждую цель
            foreach (var goalDto in request.Goals)
            {
                try
                {
                    // Проверяем уникальность названия
                    var isTitleUnique = await _goalRepository.IsGoalTitleUniqueForUserAsync(
                        request.UserId, goalDto.Title, cancellationToken);
                    
                    if (!isTitleUnique)
                    {
                        results.Items.Add(new BulkOperationItem<GoalDto>
                        {
                            Success = false,
                            Message = $"Goal title '{goalDto.Title}' is not unique for user",
                            Error = "Duplicate goal title"
                        });
                        results.FailedCount++;
                        continue;
                    }

                    // Создаем цель
                    var goal = _mapper.Map<Goal>(goalDto);
                    goal.UserId = request.UserId;

                    var createdGoal = await _goalRepository.CreateAsync(goal, cancellationToken);
                    var goalDtoResult = _mapper.Map<GoalDto>(createdGoal);

                    results.Items.Add(new BulkOperationItem<GoalDto>
                    {
                        Data = goalDtoResult,
                        Success = true,
                        Message = "Goal created successfully",
                        ItemId = createdGoal.Id
                    });
                    results.SuccessfulCount++;

                    _logger.LogDebug("Successfully created goal {GoalId} with title '{Title}'", 
                        createdGoal.Id, goalDto.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create goal with title '{Title}'", goalDto.Title);
                    
                    results.Items.Add(new BulkOperationItem<GoalDto>
                    {
                        Success = false,
                        Message = $"Failed to create goal: {ex.Message}",
                        Error = ex.Message
                    });
                    results.FailedCount++;
                }
            }

            _logger.LogInformation("Bulk creation completed: {SuccessfulCount} successful, {FailedCount} failed", 
                results.SuccessfulCount, results.FailedCount);

            return results;
        }
    }

    public class BulkUpdateGoalsCommandHandler : IRequestHandler<BulkUpdateGoalsCommand, BulkOperationResult<GoalDto>>
    {
        private readonly IGoalRepository _goalRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<BulkUpdateGoalsCommandHandler> _logger;

        public BulkUpdateGoalsCommandHandler(
            IGoalRepository goalRepository,
            IMapper mapper,
            ILogger<BulkUpdateGoalsCommandHandler> logger)
        {
            _goalRepository = goalRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BulkOperationResult<GoalDto>> Handle(
            BulkUpdateGoalsCommand request, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting bulk update of {GoalCount} goals", request.Goals.Count);

            var results = new BulkOperationResult<GoalDto>
            {
                TotalCount = request.Goals.Count
            };

            foreach (var updateItem in request.Goals)
            {
                try
                {
                    var existingGoal = await _goalRepository.GetByIdAsync(updateItem.GoalId, cancellationToken);
                    if (existingGoal == null)
                    {
                        results.Items.Add(new BulkOperationItem<GoalDto>
                        {
                            Success = false,
                            Message = $"Goal with ID {updateItem.GoalId} not found",
                            Error = "Goal not found",
                            ItemId = updateItem.GoalId
                        });
                        results.FailedCount++;
                        continue;
                    }

                    // Обновляем только указанные поля
                    if (!string.IsNullOrEmpty(updateItem.UpdateData.Title))
                        existingGoal.Title = updateItem.UpdateData.Title;

                    if (!string.IsNullOrEmpty(updateItem.UpdateData.Description))
                        existingGoal.Description = updateItem.UpdateData.Description;

                    if (!string.IsNullOrEmpty(updateItem.UpdateData.Category))
                        existingGoal.Category = Enum.Parse<GoalCategory>(updateItem.UpdateData.Category);

                    if (!string.IsNullOrEmpty(updateItem.UpdateData.Priority))
                        existingGoal.Priority = Enum.Parse<GoalPriority>(updateItem.UpdateData.Priority);

                    if (updateItem.UpdateData.DueDate.HasValue)
                        existingGoal.DueDate = updateItem.UpdateData.DueDate.Value;

                    if (updateItem.UpdateData.TargetValue.HasValue)
                        existingGoal.TargetValue = updateItem.UpdateData.TargetValue.Value;

                    existingGoal.UpdatedAt = DateTime.UtcNow;

                    var updatedGoal = await _goalRepository.UpdateAsync(existingGoal, cancellationToken);
                    var goalDto = _mapper.Map<GoalDto>(updatedGoal);

                    results.Items.Add(new BulkOperationItem<GoalDto>
                    {
                        Data = goalDto,
                        Success = true,
                        Message = "Goal updated successfully",
                        ItemId = updateItem.GoalId
                    });
                    results.SuccessfulCount++;

                    _logger.LogDebug("Successfully updated goal {GoalId}", updateItem.GoalId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update goal {GoalId}", updateItem.GoalId);
                    
                    results.Items.Add(new BulkOperationItem<GoalDto>
                    {
                        Success = false,
                        Message = $"Failed to update goal: {ex.Message}",
                        Error = ex.Message,
                        ItemId = updateItem.GoalId
                    });
                    results.FailedCount++;
                }
            }

            _logger.LogInformation("Bulk update completed: {SuccessfulCount} successful, {FailedCount} failed", 
                results.SuccessfulCount, results.FailedCount);

            return results;
        }
    }

    public class BulkDeleteGoalsCommandHandler : IRequestHandler<BulkDeleteGoalsCommand, BulkDeleteResult>
    {
        private readonly IGoalRepository _goalRepository;
        private readonly ILogger<BulkDeleteGoalsCommandHandler> _logger;

        public BulkDeleteGoalsCommandHandler(
            IGoalRepository goalRepository,
            ILogger<BulkDeleteGoalsCommandHandler> logger)
        {
            _goalRepository = goalRepository;
            _logger = logger;
        }

        public async Task<BulkDeleteResult> Handle(
            BulkDeleteGoalsCommand request, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting bulk deletion of {GoalCount} goals", request.GoalIds.Count);

            var results = new BulkDeleteResult
            {
                TotalCount = request.GoalIds.Count
            };

            foreach (var goalId in request.GoalIds)
            {
                try
                {
                    var success = await _goalRepository.DeleteAsync(goalId, cancellationToken);
                    
                    if (success)
                    {
                        results.Items.Add(new BulkDeleteItem
                        {
                            Id = goalId,
                            Success = true,
                            Message = "Goal deleted successfully"
                        });
                        results.SuccessfulCount++;
                        _logger.LogDebug("Successfully deleted goal {GoalId}", goalId);
                    }
                    else
                    {
                        results.Items.Add(new BulkDeleteItem
                        {
                            Id = goalId,
                            Success = false,
                            Message = "Goal not found",
                            Error = "Goal not found"
                        });
                        results.FailedCount++;
                        _logger.LogWarning("Goal {GoalId} not found for deletion", goalId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete goal {GoalId}", goalId);
                    
                    results.Items.Add(new BulkDeleteItem
                    {
                        Id = goalId,
                        Success = false,
                        Message = $"Failed to delete goal: {ex.Message}",
                        Error = ex.Message
                    });
                    results.FailedCount++;
                }
            }

            _logger.LogInformation("Bulk deletion completed: {SuccessfulCount} successful, {FailedCount} failed", 
                results.SuccessfulCount, results.FailedCount);

            return results;
        }
    }
}