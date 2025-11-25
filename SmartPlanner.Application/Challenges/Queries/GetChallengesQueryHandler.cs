using AutoMapper;
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Interfaces.Repositories;

namespace SmartPlanner.Application.Challenges.Queries
{
    public class GetChallengesQueryHandler : IRequestHandler<GetChallengesQuery, List<ChallengeDto>>
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly IMapper _mapper;

        public GetChallengesQueryHandler(
            IChallengeRepository challengeRepository,
            IMapper mapper)
        {
            _challengeRepository = challengeRepository;
            _mapper = mapper;
        }

        public async Task<List<ChallengeDto>> Handle(
            GetChallengesQuery request, 
            CancellationToken cancellationToken)
        {
            var challenges = await _challengeRepository.GetAllAsync(cancellationToken);
            
            // Применяем фильтры
            var filteredChallenges = challenges.AsEnumerable();

            if (request.ActiveOnly)
            {
                filteredChallenges = filteredChallenges.Where(c => c.IsActive);
            }

            if (request.UserId.HasValue)
            {
                filteredChallenges = filteredChallenges.Where(c => 
                    c.CreatedBy == request.UserId.Value || 
                    c.Participants.Any(p => p.UserId == request.UserId.Value));
            }

            if (!string.IsNullOrEmpty(request.Type))
            {
                filteredChallenges = filteredChallenges.Where(c => 
                    c.Type.ToString().Equals(request.Type, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsGroupChallenge.HasValue)
            {
                filteredChallenges = filteredChallenges.Where(c => 
                    c.IsGroupChallenge == request.IsGroupChallenge.Value);
            }

            return _mapper.Map<List<ChallengeDto>>(filteredChallenges.ToList());
        }
    }
}