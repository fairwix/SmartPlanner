// SmartPlanner.API/Controllers/GoalsBulkController.cs
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.API.Dtos.GoalsBulk;
using SmartPlanner.Application.Common;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.API.Controllers;

[ApiController]
[Route("api/goals")]
public class GoalsBulkController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GoalsBulkController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<GoalDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<GoalDto>>), 400)]
    public async Task<ActionResult<ApiResponse<BulkOperationResult<GoalDto>>>> CreateGoalsBulk(
        [FromBody] BulkCreateGoalsRequest request,
        CancellationToken cancellationToken = default)
    {
        // ✅ AutoMapper преобразует DTO в Command
        var command = _mapper.Map<BulkCreateGoalsCommand>(request);
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
        [FromBody] BulkUpdateGoalsRequest request,
        CancellationToken cancellationToken = default)
    {
        // ✅ AutoMapper преобразует DTO в Command
        var command = _mapper.Map<BulkUpdateGoalsCommand>(request);
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
        [FromBody] BulkDeleteGoalsRequest request,
        CancellationToken cancellationToken = default)
    {
        // ✅ AutoMapper преобразует DTO в Command
        var command = _mapper.Map<BulkDeleteGoalsCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        var message = result.FailedCount == 0
            ? $"All {result.TotalCount} goals deleted successfully"
            : $"Bulk deletion completed: {result.SuccessfulCount} successful, {result.FailedCount} failed";

        return Ok(ApiResponse<BulkDeleteResult>.SuccessResult(result, message));
    }
}
