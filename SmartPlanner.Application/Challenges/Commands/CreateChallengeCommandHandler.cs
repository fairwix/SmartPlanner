using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;

namespace SmartPlanner.Application.Challenges.Commands
{
    public class CreateChallengeCommandHandler : IRequestHandler<CreateChallengeCommand, ChallengeDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateChallengeCommandHandler> _logger;

        public CreateChallengeCommandHandler(
            IApplicationDbContext context,
            IMapper mapper,
            ILogger<CreateChallengeCommandHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ChallengeDto> Handle(
            CreateChallengeCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating challenge: {Title} for user {UserId}",
                request.Title, request.CreatedBy);

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == request.CreatedBy, cancellationToken);

            if (!userExists)
                throw new ArgumentException($"User with ID {request.CreatedBy} not found");

            var challenge = new Challenge
            {
                Title = request.Title,
                Description = request.Description,
                Type = Enum.Parse<ChallengeType>(request.Type),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsGroupChallenge = request.IsGroupChallenge,
                TargetValue = request.TargetValue,
                CurrentValue = 0,
                CreatedBy = request.CreatedBy
            };

            await _context.Challenges.AddAsync(challenge, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Challenge {ChallengeId} created successfully", challenge.Id);

            return _mapper.Map<ChallengeDto>(challenge);
        }
    }
}
