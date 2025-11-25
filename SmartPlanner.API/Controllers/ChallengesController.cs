// SmartPlanner.API/Controllers/ChallengesController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Challenges.Commands;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Challenges.Queries;
using SmartPlanner.Application.Common;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChallengesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ChallengesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ChallengeDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ChallengeDto>>>> GetChallenges(
            [FromQuery] Guid? userId = null,
            [FromQuery] bool? active = null,
            [FromQuery] string? type = null,
            [FromQuery] bool? isGroupChallenge = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetChallengesQuery 
            { 
                UserId = userId, 
                ActiveOnly = active ?? false,
                Type = type,
                IsGroupChallenge = isGroupChallenge
            };
            
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<ChallengeDto>>.SuccessResult(result, "Challenges retrieved successfully"));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 404)]
        public async Task<ActionResult<ApiResponse<ChallengeDto>>> GetChallenge(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetChallengeByIdQuery { ChallengeId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<ChallengeDto>.ErrorResult("Challenge not found"));
                
            return Ok(ApiResponse<ChallengeDto>.SuccessResult(result, "Challenge retrieved successfully"));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 400)]
        public async Task<ActionResult<ApiResponse<ChallengeDto>>> CreateChallenge(
            [FromBody] CreateChallengeCommand command, 
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetChallenge), new { id = result.Id }, 
                ApiResponse<ChallengeDto>.SuccessResult(result, "Challenge created successfully"));
        }

        // ✅ ИСПРАВЛЕНО: Убрали мутацию команды, используем параметры из route
        [HttpPost("{challengeId:guid}/join/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 400)]
        public async Task<ActionResult<ApiResponse<ChallengeDto>>> JoinChallenge(
            Guid challengeId, 
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var command = new JoinChallengeCommand 
            { 
                ChallengeId = challengeId, 
                UserId = userId 
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<ChallengeDto>.SuccessResult(result, "Joined challenge successfully"));
        }

        // ✅ ИСПРАВЛЕНО: Убрали мутацию команды, используем параметры из route
        [HttpPost("{challengeId:guid}/progress/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ChallengeDto>), 400)]
        public async Task<ActionResult<ApiResponse<ChallengeDto>>> UpdateProgress(
            Guid challengeId, 
            Guid userId,
            [FromBody] UpdateChallengeProgressRequest request, // ✅ Новый DTO для прогресса
            CancellationToken cancellationToken = default)
        {
            var command = new UpdateChallengeProgressCommand 
            { 
                ChallengeId = challengeId,
                UserId = userId,
                Progress = request.Progress
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<ChallengeDto>.SuccessResult(result, "Challenge progress updated successfully"));
        }

        // ✅ ДОБАВЛЕНО: Метод для выхода из челленджа
        [HttpPost("{challengeId:guid}/leave/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<ActionResult<ApiResponse<bool>>> LeaveChallenge(
            Guid challengeId, 
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var command = new LeaveChallengeCommand 
            { 
                ChallengeId = challengeId, 
                UserId = userId 
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(result, "Left challenge successfully"));
        }

        // ✅ ДОБАВЛЕНО: Получение челленджей конкретного пользователя
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<List<ChallengeDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ChallengeDto>>>> GetUserChallenges(
            Guid userId,
            [FromQuery] bool includeCompleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserChallengesQuery 
            { 
                UserId = userId,
                IncludeCompleted = includeCompleted
            };
            
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<ChallengeDto>>.SuccessResult(result, "User challenges retrieved successfully"));
        }
    }

    // ✅ ДОБАВЛЕНО: DTO для обновления прогресса
    public class UpdateChallengeProgressRequest
    {
        public int Progress { get; set; }
    }
}