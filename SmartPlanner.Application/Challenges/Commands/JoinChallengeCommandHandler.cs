using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Challenges.Commands
{
    public class JoinChallengeCommandHandler : IRequestHandler<JoinChallengeCommand, ChallengeDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<JoinChallengeCommandHandler> _logger;

        public JoinChallengeCommandHandler(
            IApplicationDbContext context,
            IMapper mapper,
            ILogger<JoinChallengeCommandHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ChallengeDto> Handle(
            JoinChallengeCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserId} joining challenge {ChallengeId}",
                request.UserId, request.ChallengeId);

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);

            if (!userExists)
                throw new ArgumentException($"User with ID {request.UserId} not found");

            var challenge = await _context.Challenges
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, cancellationToken);

            if (challenge == null)
                throw new ArgumentException($"Challenge with ID {request.ChallengeId} not found");

            // Проверяем, не участвует ли уже
            var alreadyParticipating = challenge.Participants
                .Any(p => p.UserId == request.UserId);

            if (alreadyParticipating)
                throw new InvalidOperationException("User already participating in challenge");

            // Добавляем участника
            var participant = new ChallengeParticipant
            {
                ChallengeId = request.ChallengeId,
                UserId = request.UserId,
                JoinedAt = DateTime.UtcNow
            };

            challenge.Participants.Add(participant);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully joined challenge {ChallengeId}",
                request.UserId, request.ChallengeId);

            return _mapper.Map<ChallengeDto>(challenge);
        }
    }
}
