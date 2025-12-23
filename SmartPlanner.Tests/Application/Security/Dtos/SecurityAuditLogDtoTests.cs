// Tests/Application/Security/Dtos/SecurityAuditLogDtoTests.cs
using FluentAssertions;
using SmartPlanner.Application.Security.Dtos;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Security.Dtos;

public class SecurityAuditLogDtoTests
{
    [Fact]
    public void SecurityAuditLogDto_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var details = new { Action = "Login", Success = true };
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var createdAt = DateTime.UtcNow;

        // Act
        // Используем фактический конструктор из вашего кода
        // Из файла: public record SecurityAuditLogDto(
        //     Guid Id,
        //     string EventType,
        //     Guid? UserId,
        //     string? Email,
        //     string IpAddress,
        //     string? UserAgent,
        //     bool Success,
        //     object? Details,
        //     DateTime Timestamp,
        //     DateTime CreatedAt);

        var dto = new SecurityAuditLogDto(
            Id: id,
            EventType: "Login",
            UserId: userId,
            Email: "test@example.com",
            IpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0",
            Success: true,
            Details: details,
            Timestamp: timestamp,
            CreatedAt: createdAt);

        // Assert
        dto.Id.Should().Be(id);
        dto.EventType.Should().Be("Login");
        dto.UserId.Should().Be(userId);
        dto.Email.Should().Be("test@example.com");
        dto.IpAddress.Should().Be("127.0.0.1");
        dto.UserAgent.Should().Be("Mozilla/5.0");
        dto.Success.Should().BeTrue();
        dto.Details.Should().Be(details);
        dto.Timestamp.Should().Be(timestamp);
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SecurityAuditLogDto_CanHaveNullProperties()
    {
        // Arrange & Act
        var dto = new SecurityAuditLogDto(
            Id: Guid.NewGuid(),
            EventType: "SystemEvent",
            UserId: null,
            Email: null,
            IpAddress: "127.0.0.1",
            UserAgent: null,
            Success: true,
            Details: null,
            Timestamp: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow);

        // Assert
        dto.UserId.Should().BeNull();
        dto.Email.Should().BeNull();
        dto.UserAgent.Should().BeNull();
        dto.Details.Should().BeNull();
    }
}
