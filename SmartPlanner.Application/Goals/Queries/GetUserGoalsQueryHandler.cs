using AutoMapper;
using MediatR;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Goals.Queries
{
    public class GetUserGoalsQueryHandler : 
        IRequestHandler<GetUserGoalsQuery, PagedResult<GoalDto>>,
        IRequestHandler<GetUserGoalsAdvancedQuery, PagedResult<GoalDto>>
    {
        private readonly IGoalRepository _goalRepository;
        private readonly IMapper _mapper;

        public GetUserGoalsQueryHandler(IGoalRepository goalRepository, IMapper mapper)
        {
            _goalRepository = goalRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<GoalDto>> Handle(GetUserGoalsQuery request, CancellationToken cancellationToken)
        {
            var pagination = new PaginationRequest(request.PageNumber, request.PageSize);
            
            var result = await _goalRepository.GetUserGoalsWithPaginationAsync(
                userId: request.UserId,
                pagination: pagination,
                category: request.Category,
                priority: request.Priority,
                completed: request.Completed,
                searchTerm: request.Search,
                cancellationToken: cancellationToken);

            var goalDtos = _mapper.Map<List<GoalDto>>(result.Items);
            
            return new PagedResult<GoalDto>(goalDtos, result.TotalCount, result.PageNumber, result.PageSize);
        }

        public async Task<PagedResult<GoalDto>> Handle(GetUserGoalsAdvancedQuery request, CancellationToken cancellationToken)
        {
            var result = await _goalRepository.GetUserGoalsWithAdvancedFilteringAsync(
                userId: request.UserId,
                pagination: request.Pagination,
                cancellationToken: cancellationToken);

            var goalDtos = _mapper.Map<List<GoalDto>>(result.Items);
            
            return new PagedResult<GoalDto>(goalDtos, result.TotalCount, request.Pagination.PageNumber, request.Pagination.PageSize);
        }
    }
}