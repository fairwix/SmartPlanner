using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Interfaces.Repositories;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Challenges.Commands;

    public class CreateChallengeCommandHandler : IRequestHandler<CreateChallengeCommand, ChallengeDto>
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateChallengeCommandHandler> _logger;

        public CreateChallengeCommandHandler(
            IChallengeRepository challengeRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<CreateChallengeCommandHandler> logger)
        {
            _challengeRepository = challengeRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ChallengeDto> Handle(
            CreateChallengeCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating challenge: {Title} for user {UserId}",
                request.Title, request.CreatedBy);

            // Проверяем существование пользователя
            var user = await _userRepository.GetByIdAsync(request.CreatedBy, cancellationToken);
            if (user == null)
            {
                throw new ArgumentException(nameof(request.CreatedBy), $"User with ID {request.CreatedBy} not found");
            }

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

            var createdChallenge = await _challengeRepository.CreateAsync(challenge, cancellationToken);

            _logger.LogInformation("Challenge {ChallengeId} created successfully", createdChallenge.Id);

            return _mapper.Map<ChallengeDto>(createdChallenge);
        }
    }
