// SmartPlanner.API/Controllers/GoalsQueryController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;

using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Goals.Queries;
using SmartPlanner.Application.Common.Dtos;
//фильтрация и пагинация целей
namespace SmartPlanner.API.Controllers;

    [ApiController]
    [Route("api/goals")]
    public class GoalsQueryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GoalsQueryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<GoalDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<GoalDto>>> GetGoals(
            [FromQuery] Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? category = null,
            [FromQuery] string? priority = null,
            [FromQuery] bool? completed = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
                return BadRequest("Page number must be greater than 0");

            if (pageSize < 1 || pageSize > 100)
                return BadRequest("Page size must be between 1 and 100");

            var query = new GetUserGoalsQuery
            {
                UserId = userId,
                PageNumber = page,
                PageSize = pageSize,
                Category = category,
                Priority = priority,
                Completed = completed,
                Search = search,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("advanced")]
        [ProducesResponseType(typeof(PagedResult<GoalDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<GoalDto>>> GetGoalsAdvanced(
            [FromQuery] Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null,
            [FromQuery] DateTime? createdFrom = null,
            [FromQuery] DateTime? createdTo = null,
            [FromQuery] DateTime? dueDateFrom = null,
            [FromQuery] DateTime? dueDateTo = null,
            [FromQuery] string[]? categories = null,
            [FromQuery] string[]? priorities = null,
            [FromQuery] bool? isCompleted = null,
            [FromQuery] bool? isExpired = null,
            [FromQuery] bool? isOnTrack = null,
            [FromQuery] int? minProgress = null,
            [FromQuery] int? maxProgress = null,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
                return BadRequest("Page number must be greater than 0");

            if (pageSize < 1 || pageSize > 100)
                return BadRequest("Page size must be between 1 and 100");

            var pagination = new AdvancedPaginationRequest
            {
                PageNumber = page,
                PageSize = pageSize,
                Search = search,
                SortBy = sortBy,
                SortOrder = sortOrder,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                DueDateFrom = dueDateFrom,
                DueDateTo = dueDateTo,
                Categories = categories,
                Priorities = priorities,
                IsCompleted = isCompleted,
                IsExpired = isExpired,
                IsOnTrack = isOnTrack,
                MinProgress = minProgress,
                MaxProgress = maxProgress
            };

            var query = new GetUserGoalsAdvancedQuery
            {
                UserId = userId,
                Pagination = pagination
            };

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
    }
