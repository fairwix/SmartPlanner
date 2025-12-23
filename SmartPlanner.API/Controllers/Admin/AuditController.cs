using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Security.Dtos;
using SmartPlanner.Application.Security.Queries;

namespace SmartPlanner.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/audit")]
    [Authorize(Policy = "AdminOnly")]
    public class AuditController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SecurityAuditLogDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<PagedResult<SecurityAuditLogDto>>> GetAuditLogs(
            [FromQuery] GetAuditLogsQuery query,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(PagedResult<SecurityAuditLogDto>), 200)]
        public async Task<ActionResult<PagedResult<SecurityAuditLogDto>>> GetUserAuditLogs(
            Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserAuditLogsQuery
            {
                UserId = userId,
                PageNumber = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("suspicious")]
        [ProducesResponseType(typeof(List<SecurityAuditLogDto>), 200)]
        public async Task<ActionResult<List<SecurityAuditLogDto>>> GetSuspiciousActivity(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetSuspiciousActivityQuery
            {
                From = from ?? DateTime.UtcNow.AddDays(-7),
                To = to ?? DateTime.UtcNow
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(AuditSummaryDto), 200)]
        public async Task<ActionResult<AuditSummaryDto>> GetAuditSummary(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAuditSummaryQuery
            {
                From = from ?? DateTime.UtcNow.AddDays(-30),
                To = to ?? DateTime.UtcNow
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
