    // SmartPlanner.API/Controllers/AchievementsController.cs
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using SmartPlanner.Application.Achievements.Commands;
    using SmartPlanner.Application.Achievements.Dtos;
    using SmartPlanner.Application.Achievements.Queries;

    namespace SmartPlanner.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class AchievementsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AchievementsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AchievementDto>), 200)]
        public async Task<ActionResult<List<AchievementDto>>> GetAchievements(
            [FromQuery] string? type = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAchievementsQuery { AchievementType = type };
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AchievementDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<AchievementDto>> GetAchievement(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAchievementByIdQuery { AchievementId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(List<UserAchievementDto>), 200)]
        public async Task<ActionResult<List<UserAchievementDto>>> GetUserAchievements(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserAchievementsQuery { UserId = userId };
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpPost("user/{userId:guid}/check")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> CheckAndAwardAchievements(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var command = new CheckAndAwardAchievementsCommand { UserId = userId };
            await _mediator.Send(command, cancellationToken);

            return Ok();
        }

        [HttpPost("user/{userId:guid}/award/{achievementId:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> AwardAchievement(
            Guid userId,
            Guid achievementId,
            CancellationToken cancellationToken = default)
        {
            var command = new AwardAchievementCommand
            {
                UserId = userId,
                AchievementId = achievementId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
