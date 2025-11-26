// SmartPlanner.API/Controllers/UsersController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;

using SmartPlanner.Application.Users.Commands;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Application.Users.Queries;
//управление пользователями
namespace SmartPlanner.API.Controllers;

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
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserDto>> GetUser(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<UserDto>> CreateUser(
            [FromBody] CreateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserDto>> UpdateUser(
            Guid id,
            [FromBody] UpdateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            command.UserId = id;
            var result = await _mediator.Send(command, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("{id:guid}/friends")]
        [ProducesResponseType(typeof(List<UserDto>), 200)]
        public async Task<ActionResult<List<UserDto>>> GetUserFriends(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserFriendsQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpPost("{id:guid}/friends/{friendId:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> AddFriend(
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
            return Ok(result);
        }
    }
