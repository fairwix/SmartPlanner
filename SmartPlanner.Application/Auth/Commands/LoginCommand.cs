using MediatR;
using SmartPlanner.Application.Auth.Dtos;

namespace SmartPlanner.Application.Auth.Commands
{
    public record LoginCommand : IRequest<AuthResponseDto>
    {
        public string EmailOrUsername { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? DeviceInfo { get; init; }
        public string? IpAddress { get; init; }
    }
}
