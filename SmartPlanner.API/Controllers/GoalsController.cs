// SmartPlanner.API/Controllers/GoalsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Common;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Goals.Queries;
//работа с отдельными целями
namespace SmartPlanner.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class GoalsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GoalsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 404)]
        public async Task<ActionResult<ApiResponse<GoalDto>>> GetGoal(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetGoalByIdQuery { GoalId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
                return NotFound(ApiResponse<GoalDto>.ErrorResult("Goal not found"));

            return Ok(ApiResponse<GoalDto>.SuccessResult(result, "Goal retrieved successfully"));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 400)]
        public async Task<ActionResult<ApiResponse<GoalDto>>> CreateGoal(
            [FromBody] CreateGoalCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetGoal), new { id = result.Id },
                ApiResponse<GoalDto>.SuccessResult(result, "Goal created successfully"));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 404)]
        public async Task<ActionResult<ApiResponse<GoalDto>>> UpdateGoal(
            Guid id,
            [FromBody] UpdateGoalCommand command,
            CancellationToken cancellationToken = default)
        {
            command.GoalId = id;
            var result = await _mediator.Send(command, cancellationToken);

            if (result == null)
                return NotFound(ApiResponse<GoalDto>.ErrorResult("Goal not found"));

            return Ok(ApiResponse<GoalDto>.SuccessResult(result, "Goal updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteGoal(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var command = new DeleteGoalCommand { GoalId = id };
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResult("Goal not found"));

            return Ok(ApiResponse<bool>.SuccessResult(true, "Goal deleted successfully"));
        }

        [HttpPost("{id:guid}/progress")]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<GoalDto>), 404)]
        public async Task<ActionResult<ApiResponse<GoalDto>>> UpdateProgress(
            Guid id,
            [FromBody] UpdateGoalProgressCommand command,
            CancellationToken cancellationToken = default)
        {
            command.GoalId = id;
            var result = await _mediator.Send(command, cancellationToken);

            if (result == null)
                return NotFound(ApiResponse<GoalDto>.ErrorResult("Goal not found"));

            return Ok(ApiResponse<GoalDto>.SuccessResult(result, "Progress updated successfully"));
        }
    }
