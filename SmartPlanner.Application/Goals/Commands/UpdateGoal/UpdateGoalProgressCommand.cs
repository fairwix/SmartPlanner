// SmartPlanner.Application/Goals/Commands/UpdateGoalProgressCommand.cs
using MediatR;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Commands
{
    public record UpdateGoalProgressCommand : IRequest<GoalDto?>
    {
        public Guid GoalId { get; set; }
        public int Value { get; init; }
    }
}