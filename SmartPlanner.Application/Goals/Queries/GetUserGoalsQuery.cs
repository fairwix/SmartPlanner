using MediatR;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Goals.Dtos;

namespace SmartPlanner.Application.Goals.Queries;

    public record GetUserGoalsQuery : IRequest<PagedResult<GoalDto>>
    {
        public Guid UserId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? Category { get; init; }
        public string? Priority { get; init; }
        public bool? Completed { get; init; }
        public string? Search { get; init; }
        public string? SortBy { get; init; }
        public string? SortOrder { get; init; }
    }

    public record GetUserGoalsAdvancedQuery : IRequest<PagedResult<GoalDto>>
    {
        public Guid UserId { get; init; }
        public AdvancedPaginationRequest Pagination { get; init; } = new();
    }
