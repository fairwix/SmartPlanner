using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Challenges.Commands;

public sealed class UpdateChallengeProgressCommandHandler
    : IRequestHandler<UpdateChallengeProgressCommand, ChallengeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateChallengeProgressCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ChallengeDto> Handle(
        UpdateChallengeProgressCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Progress <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Progress), "Progress must be positive");

        var participant = await _context.ChallengeParticipants
            .Include(p => p.Challenge)
                .ThenInclude(challenge => challenge!.Participants)
                    .ThenInclude(challengeParticipant => challengeParticipant.User)
            .FirstOrDefaultAsync(p => p.ChallengeId == request.ChallengeId &&
                                      p.UserId == request.UserId &&
                                      p.Status == ParticipantStatus.Joined,
                cancellationToken);
        if (participant?.Challenge == null)
            throw new KeyNotFoundException("Active challenge participation was not found");
        if (!participant.Challenge.IsActive())
            throw new InvalidOperationException("Challenge is not active");

        participant.Challenge.UpdateProgress(request.Progress);
        participant.PersonalContribution = Math.Min(
            participant.Challenge.TargetValue,
            participant.PersonalContribution + request.Progress);
        if (participant.PersonalContribution >= participant.Challenge.TargetValue)
            participant.Status = ParticipantStatus.Completed;

        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ChallengeDto>(participant.Challenge);
    }
}

public sealed class LeaveChallengeCommandHandler : IRequestHandler<LeaveChallengeCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public LeaveChallengeCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(LeaveChallengeCommand request, CancellationToken cancellationToken)
    {
        var participant = await _context.ChallengeParticipants.FirstOrDefaultAsync(
            p => p.ChallengeId == request.ChallengeId && p.UserId == request.UserId,
            cancellationToken);
        if (participant == null || participant.Status != ParticipantStatus.Joined)
            return false;

        participant.Status = ParticipantStatus.Left;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
