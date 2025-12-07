    // SmartPlanner.API/Controllers/UsersController.cs
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using SmartPlanner.Application.Users.Commands;
    using SmartPlanner.Application.Users.Dtos;
    using SmartPlanner.Application.Users.Queries;

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
        public async Task<ActionResult<UserDto>> GetUser(Guid id)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query);
            return result != null ? Ok(result) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
        {
            var command = new CreateUserCommand
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                Interests = request.Interests ?? new List<string>()
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }
    }

    // Request DTO прямо в том же файле
    public record CreateUserRequest(string Username, string Email, string Password, List<string>? Interests);
