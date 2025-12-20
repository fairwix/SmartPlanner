using MediatR;

namespace SmartPlanner.Application.Auth.Commands
{
    public record LogoutCommand(Guid UserId) : IRequest<Unit>;
}
