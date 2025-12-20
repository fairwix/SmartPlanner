using MediatR;
using SmartPlanner.Application.Auth.Dtos;

namespace SmartPlanner.Application.Auth.Commands;

public record ConfirmEmailCommand : IRequest<EmailConfirmationResponseDto>
{
    public Guid UserId { get; init; }
    public string Token { get; init; } = string.Empty;
}

