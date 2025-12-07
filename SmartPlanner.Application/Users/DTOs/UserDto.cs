using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Users.Dtos;

public record UserDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Username,
    string Email,
    List<string> Interests,
    int Balance,
    int StreakCount,
    DateTime LastLogin) : BaseDto(Id, CreatedAt, UpdatedAt);

public record CreateUserDto(
    string Username,
    string Email,
    string Password,
    List<string> Interests);

public record UpdateUserDto(
    string? Username,
    List<string>? Interests);
