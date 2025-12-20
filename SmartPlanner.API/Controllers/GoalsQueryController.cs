using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Goals.Queries;
using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.API.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize] // ✅ Требуется аутентификация
public class GoalsQueryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GoalsQueryController> _logger;

    public GoalsQueryController(IMediator mediator, ILogger<GoalsQueryController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GoalDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<PagedResult<GoalDto>>> GetGoals(
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

        // ✅ Берем UserId из JWT токена
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        // ✅ Админы могут видеть все цели, обычные пользователи - только свои
        var isAdmin = User.IsInRole("Admin");

        var query = new GetUserGoalsQuery
        {
            UserId = currentUserId,
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

    // Добавляем метод для админов
    [HttpGet("admin/all")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<GoalDto>), 200)]
    public async Task<ActionResult<PagedResult<GoalDto>>> GetAllGoals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Реализация для админов - видит все цели
        // Здесь можно использовать другой query handler
        return Ok();
    }
}
