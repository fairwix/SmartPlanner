using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Users.Commands;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Application.Users.Queries;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.DTOs.User;

namespace SmartPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IApplicationDbContext context,
        ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        _logger.LogDebug("Getting user {UserId}", id);

        var query = new GetUserByIdQuery { UserId = id };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound();
        }

        var userIdClaim = User.FindFirst("userId")?.Value;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && (string.IsNullOrEmpty(userIdClaim) ||
            !Guid.TryParse(userIdClaim, out var currentUserId) ||
            currentUserId != id))
        {
            _logger.LogWarning("User not authorized to view profile {UserId}", id);
            return Forbid();
        }

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        _logger.LogInformation("Admin creating new user: {Username}", request.Username);

        var command = new CreateUserCommand
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            Interests = request.Interests ?? new List<string>()
        };

        var result = await _mediator.Send(command);

        _logger.LogInformation("User {UserId} created by admin", result.Id);

        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user {UserId}", id);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound();
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            User, user, "ResourceOwner");

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("User not authorized to update user {UserId}", id);
            return Forbid();
        }

        var command = new UpdateUserCommand
        {
            UserId = id,
            Username = request.Username,
            Interests = request.Interests
        };

        var result = await _mediator.Send(command);

        if (result == null)
            return NotFound();

        _logger.LogInformation("User {UserId} updated", id);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/block")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> BlockUser(Guid id)
    {
        var currentUserId = User.FindFirst("userId")?.Value;
        if (!Guid.TryParse(currentUserId, out var adminId))
            return Unauthorized();

        var command = new BlockUserCommand
        {
            UserId = id,
            BlockedBy = adminId
        };

        await _mediator.Send(command);
        return Ok(new { message = "User blocked successfully" });
    }

    [HttpPatch("{id:guid}/unblock")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UnblockUser(Guid id)
    {
        var currentUserId = User.FindFirst("userId")?.Value;
        if (!Guid.TryParse(currentUserId, out var adminId))
            return Unauthorized();

        var command = new UnblockUserCommand
        {
            UserId = id,
            UnblockedBy = adminId
        };

        await _mediator.Send(command);
        return Ok(new { message = "User unblocked successfully" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("Admin deleting user {UserId}", id);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound();
        }

        var currentUserId = User.FindFirst("userId")?.Value;
        if (currentUserId == id.ToString())
        {
            _logger.LogWarning("Admin attempted to delete own account");
            return BadRequest("Cannot delete your own account");
        }

        return Ok(new { message = "User deleted successfully" });
    }
}
