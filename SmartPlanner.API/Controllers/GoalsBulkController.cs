// SmartPlanner.API/Controllers/GoalsBulkController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;

using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.API.Controllers;

    [ApiController]
    [Route("api/goals")]
    public class GoalsBulkController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GoalsBulkController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("bulk")]
        [ProducesResponseType(typeof(BulkOperationResult<GoalDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BulkOperationResult<GoalDto>>> CreateGoalsBulk(
            [FromBody] BulkCreateGoalsCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }

        [HttpPut("bulk")]
        [ProducesResponseType(typeof(BulkOperationResult<GoalDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BulkOperationResult<GoalDto>>> UpdateGoalsBulk(
            [FromBody] BulkUpdateGoalsCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }

        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(BulkDeleteResult), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BulkDeleteResult>> DeleteGoalsBulk(
            [FromBody] BulkDeleteGoalsCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
    }
