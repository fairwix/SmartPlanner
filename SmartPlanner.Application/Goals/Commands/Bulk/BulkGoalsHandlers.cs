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

            foreach (var goalDto in request.Goals)
            {
                try
                {
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
}
