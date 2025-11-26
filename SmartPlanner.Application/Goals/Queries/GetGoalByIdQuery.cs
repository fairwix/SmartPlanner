// SmartPlanner.Application/Goals/Queries/GetGoalByIdQuery.cs
using MediatR;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Queries;

    public record GetGoalByIdQuery : IRequest<GoalDto?>
    {
        public Guid GoalId { get; init; }
    }
