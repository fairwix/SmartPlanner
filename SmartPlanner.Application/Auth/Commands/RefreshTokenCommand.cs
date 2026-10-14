using MediatR;
using SmartPlanner.Application.Auth.Dtos;

namespace SmartPlanner.Application.Auth.Commands
{
    public record RefreshTokenCommand : IRequest<AuthResponseDto>
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }
}
