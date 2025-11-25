// SmartPlanner.API/Controllers/AchievementsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Achievements.Queries;
using SmartPlanner.Application.Common;


namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AchievementsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AchievementsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<AchievementDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<AchievementDto>>>> GetAchievements(
            [FromQuery] string? type = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAchievementsQuery { AchievementType = type };
            var result = await _mediator.Send(query, cancellationToken);
            
            return Ok(ApiResponse<List<AchievementDto>>.SuccessResult(result, "Achievements retrieved successfully"));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AchievementDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AchievementDto>), 404)]
        public async Task<ActionResult<ApiResponse<AchievementDto>>> GetAchievement(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetAchievementByIdQuery { AchievementId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<AchievementDto>.ErrorResult("Achievement not found"));
                
            return Ok(ApiResponse<AchievementDto>.SuccessResult(result, "Achievement retrieved successfully"));
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<List<UserAchievementDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<UserAchievementDto>>>> GetUserAchievements(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserAchievementsQuery { UserId = userId };
            var result = await _mediator.Send(query, cancellationToken);
            
            return Ok(ApiResponse<List<UserAchievementDto>>.SuccessResult(result, "User achievements retrieved successfully"));
        }

        [HttpPost("user/{userId:guid}/check")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<ActionResult<ApiResponse<bool>>> CheckAndAwardAchievements(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            var command = new CheckAndAwardAchievementsCommand { UserId = userId };
            await _mediator.Send(command, cancellationToken);
            
            return Ok(ApiResponse<bool>.SuccessResult(true, "Achievements checked successfully"));
        }

        [HttpPost("user/{userId:guid}/award/{achievementId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<ActionResult<ApiResponse<bool>>> AwardAchievement(
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
            return Ok(ApiResponse<bool>.SuccessResult(result, "Achievement awarded successfully"));
        }
    }
}