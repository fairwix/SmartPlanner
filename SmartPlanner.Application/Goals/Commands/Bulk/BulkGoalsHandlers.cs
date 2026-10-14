using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using GoalCategory = SmartPlanner.Domain.Entities.GoalCategory;

namespace SmartPlanner.Application.Goals.Commands
{
    public class BulkCreateGoalsCommandHandler : IRequestHandler<BulkCreateGoalsCommand, BulkOperationResult<GoalDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BulkCreateGoalsCommandHandler> _logger;

        public BulkCreateGoalsCommandHandler(
            IApplicationDbContext context,
            IMapper mapper,
            ILogger<BulkCreateGoalsCommandHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BulkOperationResult<GoalDto>> Handle(
            BulkCreateGoalsCommand request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bulk creation of {GoalCount} goals for user {UserId}",
                request.Goals.Count, request.UserId);

            var results = new BulkOperationResult<GoalDto>
            {
                TotalCount = request.Goals.Count
            };

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);

            if (!userExists)
            {
                var errorMessage = $"User with ID {request.UserId} not found";
                _logger.LogWarning(errorMessage);

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

            var goalsToAdd = new List<Goal>();
            var goalResults = new List<(GoalDto? dto, bool success, string message, string? error, Guid? id)>();
            var duplicatedTitles = request.Goals
                .GroupBy(g => g.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var goalDto in request.Goals)
            {
                try
                {
                    if (duplicatedTitles.Contains(goalDto.Title.Trim()))
                    {
                        goalResults.Add((null, false,
                            $"Goal title '{goalDto.Title}' is duplicated in this request",
                            "Duplicate goal title", null));
                        continue;
                    }

                    var isTitleUnique = !await _context.Goals
                        .AnyAsync(g => g.UserId == request.UserId &&
                                      g.Title == goalDto.Title,
                               cancellationToken);

                    if (!isTitleUnique)
                    {
                        goalResults.Add((null, false,
                            $"Goal title '{goalDto.Title}' is not unique",
                            "Duplicate goal title", null));
                        continue;
                    }

                    var goal = new Goal
                    {
                        Title = goalDto.Title,
                        Description = goalDto.Description ?? string.Empty,
                        Category = Enum.Parse<GoalCategory>(goalDto.Category),
                        Priority = Enum.Parse<GoalPriority>(goalDto.Priority),
                        DueDate = goalDto.DueDate,
                        TargetValue = goalDto.TargetValue,
                        CurrentValue = 0,
                        UserId = request.UserId,
                        RewardAmount = goalDto.RewardAmount
                    };

                    goalsToAdd.Add(goal);
                    var goalDtoResult = _mapper.Map<GoalDto>(goal);
                    goalResults.Add((goalDtoResult, true, "Goal created successfully", null, goal.Id));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create goal with title '{Title}'", goalDto.Title);
                    goalResults.Add((null, false,
                        $"Failed to create goal: {ex.Message}",
                        ex.Message, null));
                }
            }

            if (goalsToAdd.Any())
            {
                await _context.Goals.AddRangeAsync(goalsToAdd, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            foreach (var result in goalResults)
            {
                results.Items.Add(new BulkOperationItem<GoalDto>
                {
                    Data = result.dto,
                    Success = result.success,
                    Message = result.message,
                    Error = result.error,
                    ItemId = result.id
                });

                if (result.success)
                    results.SuccessfulCount++;
                else
                    results.FailedCount++;
            }

            return results;
        }
    }

    public class BulkUpdateGoalsCommandHandler : IRequestHandler<BulkUpdateGoalsCommand, BulkOperationResult<GoalDto>>
    {
        private readonly IApplicationDbContext _context;

        public BulkUpdateGoalsCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<BulkOperationResult<GoalDto>> Handle(
            BulkUpdateGoalsCommand request,
            CancellationToken cancellationToken)
        {
            var result = new BulkOperationResult<GoalDto> { TotalCount = request.Goals.Count };
            foreach (var item in request.Goals)
            {
                var goal = await _context.Goals.FirstOrDefaultAsync(
                    g => g.Id == item.GoalId && g.UserId == request.UserId, cancellationToken);
                if (goal == null)
                {
                    BulkGoalHandlerHelpers.AddFailure(result, item.GoalId, "Goal not found or not owned by current user");
                    continue;
                }

                try
                {
                    var data = item.UpdateData ?? throw new ArgumentException("Update data is required");
                    var category = goal.Category;
                    var priority = goal.Priority;
                    if (data.Category != null && !Enum.TryParse(data.Category, true, out category))
                        throw new ArgumentException("Invalid category");
                    if (data.Priority != null && !Enum.TryParse(data.Priority, true, out priority))
                        throw new ArgumentException("Invalid priority");
                    if (data.DueDate.HasValue && data.DueDate.Value <= DateTime.UtcNow)
                        throw new ArgumentException("Due date must be in the future");
                    if (data.TargetValue.HasValue &&
                        (data.TargetValue.Value <= 0 || data.TargetValue.Value < goal.CurrentValue))
                        throw new ArgumentException("Target value must be positive and cannot be below current progress");

                    if (data.Title != null) goal.Title = data.Title;
                    if (data.Description != null) goal.Description = data.Description;
                    goal.Category = category;
                    goal.Priority = priority;
                    if (data.DueDate.HasValue) goal.DueDate = data.DueDate.Value;
                    if (data.TargetValue.HasValue) goal.TargetValue = data.TargetValue.Value;
                    goal.UpdatedAt = DateTime.UtcNow;

                    result.Items.Add(new BulkOperationItem<GoalDto>
                    {
                        Data = BulkGoalHandlerHelpers.ToDto(goal), Success = true,
                        Message = "Goal updated successfully", ItemId = goal.Id
                    });
                    result.SuccessfulCount++;
                }
                catch (ArgumentException ex)
                {
                    BulkGoalHandlerHelpers.AddFailure(result, item.GoalId, ex.Message);
                }
            }

            if (result.SuccessfulCount > 0)
                await _context.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    public class BulkDeleteGoalsCommandHandler : IRequestHandler<BulkDeleteGoalsCommand, BulkDeleteResult>
    {
        private readonly IApplicationDbContext _context;

        public BulkDeleteGoalsCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<BulkDeleteResult> Handle(
            BulkDeleteGoalsCommand request,
            CancellationToken cancellationToken)
        {
            var result = new BulkDeleteResult { TotalCount = request.GoalIds.Count };
            var seen = new HashSet<Guid>();
            foreach (var goalId in request.GoalIds)
            {
                if (!seen.Add(goalId))
                {
                    result.Items.Add(new BulkDeleteItem
                    {
                        Id = goalId, Success = false, Message = "Duplicate goal ID", Error = "Duplicate goal ID"
                    });
                    result.FailedCount++;
                    continue;
                }

                var goal = await _context.Goals.FirstOrDefaultAsync(
                    g => g.Id == goalId && g.UserId == request.UserId, cancellationToken);
                if (goal == null)
                {
                    result.Items.Add(new BulkDeleteItem
                    {
                        Id = goalId, Success = false,
                        Message = "Goal not found or not owned by current user",
                        Error = "Goal not found or not owned by current user"
                    });
                    result.FailedCount++;
                    continue;
                }

                _context.Goals.Remove(goal);
                result.Items.Add(new BulkDeleteItem
                {
                    Id = goalId, Success = true, Message = "Goal deleted successfully"
                });
                result.SuccessfulCount++;
            }

            if (result.SuccessfulCount > 0)
                await _context.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

}

internal static class BulkGoalHandlerHelpers
{
    internal static void AddFailure(BulkOperationResult<GoalDto> result, Guid goalId, string message)
    {
        result.Items.Add(new BulkOperationItem<GoalDto>
        {
            ItemId = goalId, Success = false, Message = message, Error = message
        });
        result.FailedCount++;
    }

    internal static GoalDto ToDto(Goal goal) => new(
        goal.Id, goal.CreatedAt, goal.UpdatedAt, goal.Title, goal.Description,
        goal.Category.ToString(), goal.Priority.ToString(), goal.DueDate,
        goal.TargetValue, goal.CurrentValue, goal.GetProgressPercentage(), goal.IsCompleted,
        goal.IsAiGenerated, goal.RewardAmount, goal.UserId, goal.IsExpired(), goal.IsOnTrack());
}
