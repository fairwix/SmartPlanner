using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Goals.Queries;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
[Authorize] // ✅ Весь контроллер защищен
public class GoalsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GoalsController> _logger;

    public GoalsController(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IApplicationDbContext context,
        ILogger<GoalsController> logger)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GoalDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GoalDto>> GetGoal(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting goal {GoalId}", id);

        var goal = await _context.Goals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (goal == null)
        {
            _logger.LogWarning("Goal {GoalId} not found", id);
            return NotFound();
        }

        // ✅ Проверяем права на просмотр
        var authResult = await _authorizationService.AuthorizeAsync(
            User, goal, "ResourceOwner");

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("User not authorized to view goal {GoalId}", id);
            return Forbid();
        }

        var query = new GetGoalByIdQuery { GoalId = id };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GoalDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<GoalDto>> CreateGoal(
        [FromBody] CreateGoalRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new goal");

        // ✅ Берем UserId из JWT токена, НЕ из DTO!
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            _logger.LogWarning("Invalid userId claim in token");
            return Unauthorized();
        }

        var command = new CreateGoalCommand
        {
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Category = request.Category,
            Priority = request.Priority,
            DueDate = request.DueDate,
            TargetValue = request.TargetValue,
            UserId = currentUserId // ✅ Только из токена!
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Goal {GoalId} created for user {UserId}",
            result.Id, currentUserId);

        return CreatedAtAction(nameof(GetGoal), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GoalDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GoalDto>> UpdateGoal(
        Guid id,
        [FromBody] UpdateGoalRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating goal {GoalId}", id);

        // ✅ Загружаем цель для проверки прав
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (goal == null)
        {
            _logger.LogWarning("Goal {GoalId} not found", id);
            return NotFound();
        }

        // ✅ IMPERATIVE AUTHORIZATION - проверяем права перед действием
        var authResult = await _authorizationService.AuthorizeAsync(
            User, goal, "ResourceOwner");

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("User not authorized to update goal {GoalId}", id);
            return Forbid(); // 403 Forbidden
        }

        var command = new UpdateGoalCommand
        {
            GoalId = id,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            DueDate = request.DueDate,
            TargetValue = request.TargetValue
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Goal {GoalId} updated", id);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeleteGoal(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting goal {GoalId}", id);

        // ✅ Загружаем цель для проверки прав
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (goal == null)
        {
            _logger.LogWarning("Goal {GoalId} not found", id);
            return NotFound();
        }

        // ✅ IMPERATIVE AUTHORIZATION
        var authResult = await _authorizationService.AuthorizeAsync(
            User, goal, "ResourceOwner");

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("User not authorized to delete goal {GoalId}", id);
            return Forbid();
        }

        var command = new DeleteGoalCommand { GoalId = id };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            _logger.LogWarning("Failed to delete goal {GoalId}", id);
            return NotFound();
        }

        _logger.LogInformation("Goal {GoalId} deleted", id);
        return Ok();
    }

    [HttpPost("{id:guid}/progress")]
    [ProducesResponseType(typeof(GoalDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GoalDto>> UpdateProgress(
        Guid id,
        [FromBody] UpdateGoalProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating progress for goal {GoalId}", id);

        // ✅ Загружаем цель для проверки прав
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (goal == null)
        {
            _logger.LogWarning("Goal {GoalId} not found", id);
            return NotFound();
        }

        // ✅ IMPERATIVE AUTHORIZATION
        var authResult = await _authorizationService.AuthorizeAsync(
            User, goal, "ResourceOwner");

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("User not authorized to update progress for goal {GoalId}", id);
            return Forbid();
        }

        var command = new UpdateGoalProgressCommand
        {
            GoalId = id,
            Value = request.Value
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Failed to update progress for goal {GoalId}", id);
            return NotFound();
        }

        _logger.LogInformation("Progress updated for goal {GoalId}: {Value}",
            id, request.Value);

        return Ok(result);
    }
}

// ✅ УДАЛИЛИ UserId из DTO - теперь берем только из токена
public record CreateGoalRequest(
    string Title,
    string? Description,
    string Category,
    string Priority,
    DateTime DueDate,
    int TargetValue);

public record UpdateGoalRequest(
    string? Title,
    string? Description,
    string? Category,
    string? Priority,
    DateTime? DueDate,
    int? TargetValue);

public record UpdateGoalProgressRequest(int Value);
