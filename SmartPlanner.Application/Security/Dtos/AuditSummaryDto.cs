namespace SmartPlanner.Application.Security.Dtos;

    public record AuditSummaryDto(
        int TotalEvents,
        int SuccessfulEvents,
        int FailedEvents,
        Dictionary<string, int> EventTypeCounts,
        Dictionary<string, int> TopIps,
        Dictionary<string, int> TopUsers,
        DateTime PeriodStart,
        DateTime PeriodEnd);

