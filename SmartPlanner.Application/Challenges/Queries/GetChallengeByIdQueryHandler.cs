using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Challenges.Queries
{
    public class GetChallengeByIdQueryHandler : IRequestHandler<GetChallengeByIdQuery, ChallengeDto?>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetChallengeByIdQueryHandler(
            IApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ChallengeDto?> Handle(
            GetChallengeByIdQuery request,
            CancellationToken cancellationToken)
        {
            var challenge = await _context.Challenges
                .Include(c => c.Participants)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, cancellationToken);

            return challenge != null ? _mapper.Map<ChallengeDto>(challenge) : null;
        }
    }
}
