using MediatR;
using SmartPlanner.Application.Auth.Dtos;

namespace SmartPlanner.Application.Auth.Commands
{
    public record RegisterCommand : IRequest<AuthResponseDto>
    {
        public string Email { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? PhoneNumber { get; init; }
    }
}
