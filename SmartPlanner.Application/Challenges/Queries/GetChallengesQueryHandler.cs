using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Challenges.Queries
{
    public class GetChallengesQueryHandler : IRequestHandler<GetChallengesQuery, List<ChallengeDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetChallengesQueryHandler(
            IApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ChallengeDto>> Handle(
            GetChallengesQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Challenges
                .Include(c => c.Participants)
                .AsNoTracking();

            if (request.ActiveOnly)
            {
                var now = DateTime.UtcNow;
                query = query.Where(c => c.StartDate <= now && c.EndDate >= now);
            }

            if (request.UserId.HasValue)
            {
                query = query.Where(c =>
                    c.CreatedBy == request.UserId.Value ||
                    c.Participants.Any(p => p.UserId == request.UserId.Value));
            }

            if (!string.IsNullOrEmpty(request.Type))
            {
                if (Enum.TryParse<ChallengeType>(request.Type, true, out var type))
                {
                    query = query.Where(c => c.Type == type);
                }
            }

            if (request.IsGroupChallenge.HasValue)
            {
                query = query.Where(c => c.IsGroupChallenge == request.IsGroupChallenge.Value);
            }

            var challenges = await query.ToListAsync(cancellationToken);
            return _mapper.Map<List<ChallengeDto>>(challenges);
        }
    }
}
