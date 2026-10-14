// Tests/Application/Security/Dtos/AuditSummaryDtoTests.cs
using FluentAssertions;
using SmartPlanner.Application.Security.Dtos;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Security.Dtos;

public class AuditSummaryDtoTests
{
    [Fact]
    public void AuditSummaryDto_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var eventTypeCounts = new Dictionary<string, int>
        {
            { "Login", 10 },
            { "Logout", 5 }
        };

        var topIps = new Dictionary<string, int>
        {
            { "127.0.0.1", 8 },
            { "192.168.1.1", 7 }
        };

        var topUsers = new Dictionary<string, int>
        {
            { "user1@example.com", 6 },
            { "user2@example.com", 4 }
        };

        var periodStart = DateTime.UtcNow.AddDays(-7);
        var periodEnd = DateTime.UtcNow;

        // Act
        // Используем фактический конструктор из вашего кода
        var dto = new AuditSummaryDto(
            TotalEvents: 15,
            SuccessfulEvents: 12,
            FailedEvents: 3,
            EventTypeCounts: eventTypeCounts,
            TopIps: topIps,
            TopUsers: topUsers,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd);

        // Assert
        dto.TotalEvents.Should().Be(15);
        dto.SuccessfulEvents.Should().Be(12);
        dto.FailedEvents.Should().Be(3);
        dto.EventTypeCounts.Should().ContainKey("Login");
        dto.EventTypeCounts["Login"].Should().Be(10);
        dto.TopIps.Should().ContainKey("127.0.0.1");
        dto.TopUsers.Should().ContainKey("user1@example.com");
        dto.PeriodStart.Should().Be(periodStart);
        dto.PeriodEnd.Should().Be(periodEnd);
    }
}
