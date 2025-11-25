// SmartPlanner.API/Controllers/UsersController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Common;
using SmartPlanner.Application.Users.Commands;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Application.Users.Queries;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
                
            return Ok(ApiResponse<UserDto>.SuccessResult(result, "User retrieved successfully"));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 400)]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
            [FromBody] CreateUserCommand command, 
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, 
                ApiResponse<UserDto>.SuccessResult(result, "User created successfully"));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
            Guid id, 
            [FromBody] UpdateUserCommand command, 
            CancellationToken cancellationToken = default)
        {
            command.UserId = id;
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
                
            return Ok(ApiResponse<UserDto>.SuccessResult(result, "User updated successfully"));
        }

        [HttpGet("{id:guid}/friends")]
        [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUserFriends(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserFriendsQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            return Ok(ApiResponse<List<UserDto>>.SuccessResult(result, "Friends retrieved successfully"));
        }

        [HttpPost("{id:guid}/friends/{friendId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<ActionResult<ApiResponse<bool>>> AddFriend(
            Guid id, 
            Guid friendId, 
            CancellationToken cancellationToken = default)
        {
            var command = new AddFriendCommand 
            { 
                UserId = id, 
                FriendId = friendId 
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(result, "Friend added successfully"));
        }
    }
}