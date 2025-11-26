// SmartPlanner.API/Controllers/GoalsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
        [ProducesResponseType(typeof(GoalDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GoalDto>> GetGoal(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetGoalByIdQuery { GoalId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(GoalDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<GoalDto>> CreateGoal(
            [FromBody] CreateGoalCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetGoal), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(GoalDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GoalDto>> UpdateGoal(
            Guid id,
            [FromBody] UpdateGoalCommand command,
            CancellationToken cancellationToken = default)
        {
            command.GoalId = id;
            var result = await _mediator.Send(command, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteGoal(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var command = new DeleteGoalCommand { GoalId = id };
            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
                return NotFound();

            return Ok();
        }

        [HttpPost("{id:guid}/progress")]
        [ProducesResponseType(typeof(GoalDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GoalDto>> UpdateProgress(
            Guid id,
            [FromBody] UpdateGoalProgressCommand command,
            CancellationToken cancellationToken = default)
        {
            command.GoalId = id;
            var result = await _mediator.Send(command, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
