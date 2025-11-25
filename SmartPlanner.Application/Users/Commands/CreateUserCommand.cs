// SmartPlanner.Application/Users/Commands/CreateUserCommand.cs
using MediatR;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Commands
{
    public record CreateUserCommand : IRequest<UserDto>
    {
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public List<string> Interests { get; init; } = new();
    }
}