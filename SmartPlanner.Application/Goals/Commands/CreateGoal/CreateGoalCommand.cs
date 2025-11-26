// SmartPlanner.Application/Goals/Commands/CreateGoalCommand.cs
using MediatR;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Commands;

    public record CreateGoalCommand : IRequest<GoalDto>
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public DateTime DueDate { get; init; }
        public int TargetValue { get; init; } = 1;
        public Guid UserId { get; init; }
    }
