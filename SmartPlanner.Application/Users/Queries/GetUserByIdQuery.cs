// SmartPlanner.Application/Users/Queries/GetUserByIdQuery.cs
using MediatR;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Queries
{
    public record GetUserByIdQuery : IRequest<UserDto?>
    {
        public Guid UserId { get; init; }
    }
}