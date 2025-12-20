// Application/Security/Queries/
using MediatR;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Security.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Security.Queries
{
    public record GetAuditLogsQuery : IRequest<PagedResult<SecurityAuditLogDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 50;
        public SecurityEventType? EventType { get; init; }
        public Guid? UserId { get; init; }
        public string? IpAddress { get; init; }
        public bool? Success { get; init; }
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public string? SortBy { get; init; } = "Timestamp";
        public string? SortOrder { get; init; } = "desc";
    }

    public record GetUserAuditLogsQuery : IRequest<PagedResult<SecurityAuditLogDto>>
    {
        public Guid UserId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record GetSuspiciousActivityQuery : IRequest<List<SecurityAuditLogDto>>
    {
        public DateTime From { get; init; }
        public DateTime To { get; init; }
    }

    public record GetAuditSummaryQuery : IRequest<AuditSummaryDto>
    {
        public DateTime From { get; init; }
        public DateTime To { get; init; }
    }
}
