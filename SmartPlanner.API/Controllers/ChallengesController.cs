using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Challenges.Commands;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Challenges.Queries;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
[Authorize]
public class ChallengesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChallengesController> _logger;

    public ChallengesController(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IApplicationDbContext context,
        ILogger<ChallengesController> logger)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChallengeDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ChallengeDto>> CreateChallenge(
        [FromBody] CreateChallengeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new challenge");

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            _logger.LogWarning("Invalid userId claim in token");
            return Unauthorized();
        }

        var command = new CreateChallengeCommand
        {
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsGroupChallenge = request.IsGroupChallenge,
            TargetValue = request.TargetValue,
            CreatedBy = currentUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Challenge {ChallengeId} created by user {UserId}",
            result.Id, currentUserId);

        return CreatedAtAction(nameof(GetChallenge), new { id = result.Id }, result);
    }

    [HttpPost("{challengeId:guid}/join")]
    [ProducesResponseType(typeof(ChallengeDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ChallengeDto>> JoinChallenge(
        Guid challengeId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Joining challenge {ChallengeId}", challengeId);

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var challenge = await _context.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken);

        if (challenge == null)
        {
            _logger.LogWarning("Challenge {ChallengeId} not found", challengeId);
            return NotFound();
        }

        var command = new JoinChallengeCommand
        {
            ChallengeId = challengeId,
            UserId = currentUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("User {UserId} joined challenge {ChallengeId}",
            currentUserId, challengeId);

        return Ok(result);
    }

    [HttpPost("{challengeId:guid}/progress")]
    [ProducesResponseType(typeof(ChallengeDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ChallengeDto>> UpdateProgress(
        Guid challengeId,
        [FromBody] UpdateChallengeProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating progress for challenge {ChallengeId}", challengeId);

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var participant = await _context.ChallengeParticipants
            .Include(p => p.Challenge)
            .FirstOrDefaultAsync(p =>
                p.ChallengeId == challengeId &&
                p.UserId == currentUserId,
                cancellationToken);

        if (participant == null)
        {
            _logger.LogWarning("User {UserId} not participating in challenge {ChallengeId}",
                currentUserId, challengeId);
            return NotFound();
        }

        if (participant.Challenge == null || !participant.Challenge.IsActive())
        {
            _logger.LogWarning("Challenge {ChallengeId} not active", challengeId);
            return BadRequest("Challenge is not active");
        }

        var command = new UpdateChallengeProgressCommand
        {
            ChallengeId = challengeId,
            Progress = request.Progress,
            UserId = currentUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Progress updated for challenge {ChallengeId} by user {UserId}",
            challengeId, currentUserId);

        return Ok(result);
    }

    [HttpPost("{challengeId:guid}/leave")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<bool>> LeaveChallenge(
        Guid challengeId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Leaving challenge {ChallengeId}", challengeId);

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var participant = await _context.ChallengeParticipants
            .FirstOrDefaultAsync(p =>
                p.ChallengeId == challengeId &&
                p.UserId == currentUserId,
                cancellationToken);

        if (participant == null)
        {
            _logger.LogWarning("User {UserId} not participating in challenge {ChallengeId}",
                currentUserId, challengeId);
            return NotFound();
        }

        var command = new LeaveChallengeCommand
        {
            ChallengeId = challengeId,
            UserId = currentUserId
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("User {UserId} left challenge {ChallengeId}",
            currentUserId, challengeId);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChallengeDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ChallengeDto>> GetChallenge(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetChallengeByIdQuery { ChallengeId = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
            return NotFound();

        if (result.CreatedBy != Guid.Parse(User.FindFirst("userId")?.Value ?? ""))
        {
            var isParticipant = await _context.ChallengeParticipants
                .AnyAsync(p => p.ChallengeId == id &&
                             p.UserId == Guid.Parse(User.FindFirst("userId").Value ?? ""),
                    cancellationToken);

            if (!isParticipant)
                return Forbid();
        }

        return Ok(result);
    }
}

public record CreateChallengeRequest(
    string Title,
    string? Description,
    string Type,
    DateTime StartDate,
    DateTime EndDate,
    bool IsGroupChallenge,
    int TargetValue);

public record UpdateChallengeProgressRequest(int Progress);
