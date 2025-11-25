// SmartPlanner.API/Controllers/GoalsBulkController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Common;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.API.Controllers
{
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
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<GoalDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<GoalDto>>), 400)]
        public async Task<ActionResult<ApiResponse<BulkOperationResult<GoalDto>>>> CreateGoalsBulk(
            [FromBody] BulkCreateGoalsCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            var message = result.AllSucceeded 
                ? $"All {result.TotalCount} goals created successfully" 
                : $"Bulk creation completed: {result.SuccessfulCount} successful, {result.FailedCount} failed";
            
            return Ok(ApiResponse<BulkOperationResult<GoalDto>>.SuccessResult(result, message));
        }

        [HttpPut("bulk")]
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<GoalDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<GoalDto>>), 400)]
        public async Task<ActionResult<ApiResponse<BulkOperationResult<GoalDto>>>> UpdateGoalsBulk(
            [FromBody] BulkUpdateGoalsCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            var message = result.AllSucceeded 
                ? $"All {result.TotalCount} goals updated successfully" 
                : $"Bulk update completed: {result.SuccessfulCount} successful, {result.FailedCount} failed";
            
            return Ok(ApiResponse<BulkOperationResult<GoalDto>>.SuccessResult(result, message));
        }

        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(ApiResponse<BulkDeleteResult>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BulkDeleteResult>), 400)]
        public async Task<ActionResult<ApiResponse<BulkDeleteResult>>> DeleteGoalsBulk(
            [FromBody] BulkDeleteGoalsCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            var message = result.FailedCount == 0 
                ? $"All {result.TotalCount} goals deleted successfully" 
                : $"Bulk deletion completed: {result.SuccessfulCount} successful, {result.FailedCount} failed";
            
            return Ok(ApiResponse<BulkDeleteResult>.SuccessResult(result, message));
        }
    }
}