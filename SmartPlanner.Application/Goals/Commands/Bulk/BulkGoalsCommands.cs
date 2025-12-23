using MediatR;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Commands;

    public record BulkCreateGoalsCommand : IRequest<BulkOperationResult<GoalDto>>
    {
        public List<CreateGoalDto> Goals { get; init; } = new();
        public Guid UserId { get; init; }
    }

    public record BulkUpdateGoalsCommand : IRequest<BulkOperationResult<GoalDto>>
    {
        public List<BulkUpdateGoalItem> Goals { get; init; } = new();
    }

    public class BulkUpdateGoalItem
    {
        public Guid GoalId { get; set; }
        public UpdateGoalDto UpdateData { get; set; }
    }

    public record BulkDeleteGoalsCommand : IRequest<BulkDeleteResult>
    {
        public List<Guid> GoalIds { get; init; } = new();
    }
