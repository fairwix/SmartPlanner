using MediatR;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Commands;

    public record UpdateGoalCommand : IRequest<GoalDto?>
    {
        public Guid GoalId { get; set; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Category { get; init; }
        public string? Priority { get; init; }
        public DateTime? DueDate { get; init; }
        public int? TargetValue { get; init; }
    }
