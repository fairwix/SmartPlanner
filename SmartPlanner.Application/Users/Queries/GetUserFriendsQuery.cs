using MediatR;
using SmartPlanner.Application.Users.Dtos;

namespace SmartPlanner.Application.Users.Queries;

    public record GetUserFriendsQuery : IRequest<List<UserDto>>
    {
        public Guid UserId { get; init; }
    }
