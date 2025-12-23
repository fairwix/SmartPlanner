using MediatR;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Commands;

    public record UpdateGoalProgressCommand : IRequest<GoalDto?>
    {
        public Guid GoalId { get; init; }
        public int Value { get; init; }
    }
