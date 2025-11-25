using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Application.Challenges.Commands
{
    public class JoinChallengeCommandHandler : IRequestHandler<JoinChallengeCommand, ChallengeDto>
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<JoinChallengeCommandHandler> _logger;

        public JoinChallengeCommandHandler(
            IChallengeRepository challengeRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<JoinChallengeCommandHandler> logger)
        {
            _challengeRepository = challengeRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ChallengeDto> Handle(
            JoinChallengeCommand request, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserId} joining challenge {ChallengeId}", 
                request.UserId, request.ChallengeId);

            // Проверяем существование пользователя
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {request.UserId} not found");
            }

            // Присоединяем к челленджу
            var success = await _challengeRepository.AddParticipantToChallengeAsync(
                request.ChallengeId, request.UserId, cancellationToken);

            if (!success)
            {
                throw new InvalidOperationException("Failed to join challenge");
            }

            // Получаем обновленный челлендж
            var challenge = await _challengeRepository.GetByIdAsync(request.ChallengeId, cancellationToken);
            if (challenge == null)
            {
                throw new ArgumentException($"Challenge with ID {request.ChallengeId} not found");
            }

            _logger.LogInformation("User {UserId} successfully joined challenge {ChallengeId}", 
                request.UserId, request.ChallengeId);

            return _mapper.Map<ChallengeDto>(challenge);
        }
    }
}