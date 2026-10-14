namespace SmartPlanner.Application.Security.Dtos;
public record SecurityAuditLogDto(
    Guid Id,
    string EventType,
    Guid? UserId,
    string? Email,
    string IpAddress,
    string? UserAgent,
    bool Success,
    object? Details,
    DateTime Timestamp,
    DateTime CreatedAt);
