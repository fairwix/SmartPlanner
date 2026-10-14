using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Achievements.Queries;

namespace SmartPlanner.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminAchievementsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AdminAchievementsController> _logger;

        public AdminAchievementsController(
            IMediator mediator,
            ILogger<AdminAchievementsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("award/{userId:guid}/{achievementId:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<bool>> AwardAchievement(
            Guid userId,
            Guid achievementId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin awarding achievement {AchievementId} to user {UserId}",
                achievementId, userId);

            var command = new AwardAchievementCommand
            {
                UserId = userId,
                AchievementId = achievementId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                _logger.LogWarning("Failed to award achievement {AchievementId} to user {UserId}",
                    achievementId, userId);
                return BadRequest("Failed to award achievement");
            }

            _logger.LogInformation("Achievement {AchievementId} awarded to user {UserId}",
                achievementId, userId);

            return Ok(result);
        }
    }
}


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AchievementsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AchievementsController> _logger;

    public AchievementsController(IMediator mediator, ILogger<AchievementsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<AchievementDto>), 200)]
    public async Task<ActionResult<List<AchievementDto>>> GetAchievements(
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAchievementsQuery { AchievementType = type };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(List<UserAchievementDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<List<UserAchievementDto>>> GetUserAchievements(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting achievements for user {UserId}", userId);

        var currentUserId = User.FindFirst("userId")?.Value;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && (string.IsNullOrEmpty(currentUserId) ||
            !Guid.TryParse(currentUserId, out var currentId) ||
            currentId != userId))
        {
            _logger.LogWarning("User not authorized to view achievements of user {UserId}", userId);
            return Forbid();
        }

        var query = new GetUserAchievementsQuery { UserId = userId };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpPost("check")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> CheckAndAwardAchievements(
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var command = new CheckAndAwardAchievementsCommand { UserId = currentUserId };
        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Checked and awarded achievements for user {UserId}", currentUserId);

        return Ok();
    }
}
