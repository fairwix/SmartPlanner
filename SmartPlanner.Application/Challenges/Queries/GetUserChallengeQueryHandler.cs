using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Challenges.Queries
{
    public class GetUserChallengesQueryHandler : IRequestHandler<GetUserChallengesQuery, List<ChallengeDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetUserChallengesQueryHandler(
            IApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ChallengeDto>> Handle(
            GetUserChallengesQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Challenges
                .Include(c => c.Participants)
                .Where(c => c.CreatedBy == request.UserId ||
                            c.Participants.Any(p => p.UserId == request.UserId))
                .AsNoTracking();

            if (!request.IncludeCompleted)
            {
                var now = DateTime.UtcNow;
                query = query.Where(c => c.EndDate >= now);
            }

            var challenges = await query.ToListAsync(cancellationToken);
            return _mapper.Map<List<ChallengeDto>>(challenges);
        }
    }
}
