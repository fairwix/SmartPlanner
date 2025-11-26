// SmartPlanner.Application/Goals/Commands/DeleteGoalCommand.cs
using MediatR;

namespace SmartPlanner.Application.Goals.Commands;

    public record DeleteGoalCommand : IRequest<bool>
    {
        public Guid GoalId { get; init; }
    }
