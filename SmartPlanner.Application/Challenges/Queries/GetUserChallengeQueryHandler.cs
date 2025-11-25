using MediatR;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Interfaces.Repositories;
using AutoMapper;

namespace SmartPlanner.Application.Challenges.Queries
{
    public class GetUserChallengesQueryHandler : IRequestHandler<GetUserChallengesQuery, List<ChallengeDto>>
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IMapper _mapper;

        public GetUserChallengesQueryHandler(IChallengeRepository challengeRepository, IMapper mapper)
        {
            _challengeRepository = challengeRepository;
            _mapper = mapper;
        }

        public async Task<List<ChallengeDto>> Handle(GetUserChallengesQuery request, CancellationToken cancellationToken)
        {
            var challenges = await _challengeRepository.GetUserChallengesAsync(request.UserId, cancellationToken);
            
            // Фильтрация по завершенным, если нужно
            if (!request.IncludeCompleted)
            {
                challenges = challenges.Where(c => c.IsActive && !c.IsExpired()).ToList();
            }

            return _mapper.Map<List<ChallengeDto>>(challenges);
        }
    }
}