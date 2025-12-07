using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.AI.Queries
{
    public class GeneratePersonalChallengesQueryHandler :
        IRequestHandler<GeneratePersonalChallengesQuery, List<ChallengeDto>>
    {
        private readonly IApplicationDbContext _context;

        public GeneratePersonalChallengesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChallengeDto>> Handle(
            GeneratePersonalChallengesQuery request,
            CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return new List<ChallengeDto>();

            var challenges = new List<ChallengeDto>();
            var random = new Random();

            // Берем интересы пользователя
            var interests = user.Interests?.Take(request.Count).ToList() ?? new List<string>();

            foreach (var interest in interests)
            {
                var challenge = GenerateSmartChallenge(interest, user.Id, random);
                challenges.Add(challenge);
            }

            return challenges;
        }

        private ChallengeDto GenerateSmartChallenge(string interest, Guid userId, Random random)
        {
            var title = interest.ToLower() switch
            {
                "sports" or "fitness" => $"Complete {random.Next(3, 7)} workout sessions this week",
                "reading" or "books" => $"Read {random.Next(2, 5)} books this month",
                "programming" or "coding" => $"Complete {random.Next(5, 15)} coding exercises",
                "music" => $"Practice {random.Next(10, 30)} minutes daily for a week",
                _ => $"Master {interest} in {random.Next(7, 30)} days"
            };

            return new ChallengeDto(
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow,
                title,
                $"Challenge based on your interest in {interest}",
                "Custom",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(7),
                random.Next(0, 2) == 1,
                random.Next(5, 20),
                0,
                0,
                true,
                userId,
                new List<ChallengeParticipantDto>());
        }
    }
}
