using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Goals.Queries
{
    public class GetUserGoalsQueryHandler :
        IRequestHandler<GetUserGoalsQuery, PagedResult<GoalDto>>,
        IRequestHandler<GetUserGoalsAdvancedQuery, PagedResult<GoalDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetUserGoalsQueryHandler(IApplicationDbContext context, IMapper mapper,
            ILogger<GetUserGoalsQueryHandler> mockLoggerObject)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<GoalDto>> Handle(GetUserGoalsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Goals
                .Where(g => g.UserId == request.UserId)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(request.Category))
            {
                if (Enum.TryParse<GoalCategory>(request.Category, true, out var category))
                {
                    query = query.Where(g => g.Category == category);
                }
            }

            if (!string.IsNullOrEmpty(request.Priority))
            {
                if (Enum.TryParse<GoalPriority>(request.Priority, true, out var priority))
                {
                    query = query.Where(g => g.Priority == priority);
                }
            }

            if (request.Completed.HasValue)
                query = query.Where(g => g.IsCompleted == request.Completed.Value);

            if (!string.IsNullOrEmpty(request.Search))
                query = query.Where(g => g.Title.Contains(request.Search) ||
                                       (g.Description != null && g.Description.Contains(request.Search)));

            var totalCount = await query.CountAsync(cancellationToken);

            var orderedQuery = ApplySorting(query, request.SortBy, request.SortOrder);

            var goals = await orderedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var goalDtos = _mapper.Map<List<GoalDto>>(goals);

            return new PagedResult<GoalDto>(goalDtos, totalCount, request.PageNumber, request.PageSize);
        }

        private IQueryable<Goal> ApplySorting(IQueryable<Goal> query, string? sortBy, string? sortOrder)
        {
            var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy?.ToLowerInvariant()) switch
            {
                "title" => isDescending ? query.OrderByDescending(g => g.Title) : query.OrderBy(g => g.Title),
                "duedate" => isDescending ? query.OrderByDescending(g => g.DueDate) : query.OrderBy(g => g.DueDate),
                "priority" => isDescending ? query.OrderByDescending(g => g.Priority) : query.OrderBy(g => g.Priority),
                "category" => isDescending ? query.OrderByDescending(g => g.Category) : query.OrderBy(g => g.Category),
                "createdat" => isDescending ? query.OrderByDescending(g => g.CreatedAt) : query.OrderBy(g => g.CreatedAt),
                "progress" => isDescending ? query.OrderByDescending(g => g.GetProgressPercentage()) : query.OrderBy(g => g.GetProgressPercentage()),
                _ => query.OrderByDescending(g => g.CreatedAt)
            };
        }

        public async Task<PagedResult<GoalDto>> Handle(GetUserGoalsAdvancedQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Goals
                .Where(g => g.UserId == request.UserId)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var goals = await query
                .OrderByDescending(g => g.CreatedAt)
                .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
                .Take(request.Pagination.PageSize)
                .ToListAsync(cancellationToken);

            var goalDtos = _mapper.Map<List<GoalDto>>(goals);

            return new PagedResult<GoalDto>(goalDtos, totalCount, request.Pagination.PageNumber, request.Pagination.PageSize);
        }
    }
}
