// SmartPlanner.API/Controllers/ChallengesController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Challenges.Commands;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Challenges.Queries;

//управление челленджамищщ
namespace SmartPlanner.API.Controllers;

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
        [ProducesResponseType(typeof(List<ChallengeDto>), 200)]
        public async Task<ActionResult<List<ChallengeDto>>> GetChallenges(
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
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ChallengeDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ChallengeDto>> GetChallenge(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetChallengeByIdQuery { ChallengeId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ChallengeDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ChallengeDto>> CreateChallenge(
            [FromBody] CreateChallengeCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetChallenge), new { id = result.Id }, result);
        }

        // ✅ ИСПРАВЛЕНО: Убрали мутацию команды, используем параметры из route
        [HttpPost("{challengeId:guid}/join/{userId:guid}")]
        [ProducesResponseType(typeof(ChallengeDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ChallengeDto>> JoinChallenge(
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
            return Ok(result);
        }

        // ✅ ИСПРАВЛЕНО: Убрали мутацию команды, используем параметры из route
        [HttpPost("{challengeId:guid}/progress/{userId:guid}")]
        [ProducesResponseType(typeof(ChallengeDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ChallengeDto>> UpdateProgress(
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
            return Ok(result);
        }

        // ✅ ДОБАВЛЕНО: Метод для выхода из челленджа
        [HttpPost("{challengeId:guid}/leave/{userId:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> LeaveChallenge(
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
            return Ok(result);
        }

        // ✅ ДОБАВЛЕНО: Получение челленджей конкретного пользователя
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(List<ChallengeDto>), 200)]
        public async Task<ActionResult<List<ChallengeDto>>> GetUserChallenges(
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
            return Ok(result);
        }
    }
    public class UpdateChallengeProgressRequest
    {
        public int Progress { get; set; }
    }
