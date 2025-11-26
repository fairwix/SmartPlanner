using AutoMapper;
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Application.Challenges.Queries;

    public class GetChallengeByIdQueryHandler : IRequestHandler<GetChallengeByIdQuery, ChallengeDto?>
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IMapper _mapper;

        public GetChallengeByIdQueryHandler(
            IChallengeRepository challengeRepository,
            IMapper mapper)
        {
            _challengeRepository = challengeRepository;
            _mapper = mapper;
        }

        public async Task<ChallengeDto?> Handle(
            GetChallengeByIdQuery request,
            CancellationToken cancellationToken)
        {
            var challenge = await _challengeRepository.GetByIdAsync(request.ChallengeId, cancellationToken);
            return challenge != null ? _mapper.Map<ChallengeDto>(challenge) : null;
        }
    }
