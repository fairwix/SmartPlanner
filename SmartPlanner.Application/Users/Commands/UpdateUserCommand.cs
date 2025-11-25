// SmartPlanner.Application/Users/Commands/UpdateUserCommand.cs
using MediatR;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Commands
{
    public record UpdateUserCommand : IRequest<UserDto?>
    {
        public Guid UserId { get; set; }
        public string? Username { get; init; }
        public List<string>? Interests { get; init; }
    }
}