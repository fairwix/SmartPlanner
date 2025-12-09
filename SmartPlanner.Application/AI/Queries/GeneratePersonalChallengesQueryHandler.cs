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
            // Загружаем пользователя с его интересами
            var user = await _context.Users
                .Include(u => u.UserInterests)          // Включаем UserInterests
                .ThenInclude(ui => ui.Interest)         // И связанные Interest
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return new List<ChallengeDto>();

            var challenges = new List<ChallengeDto>();
            var random = new Random();

            // Получаем интересы пользователя из UserInterests
            var interests = user.UserInterests
                .Where(ui => ui.Interest != null)
                .Select(ui => ui.Interest.Name)
                .Take(request.Count)
                .ToList();

            // Если у пользователя нет интересов, используем дефолтные
            if (!interests.Any())
            {
                interests = new List<string> { "fitness", "reading", "learning" };
            }

            // Генерируем челленджи на основе интересов
            foreach (var interest in interests)
            {
                var challenge = GenerateSmartChallenge(interest, user.Id, random);
                challenges.Add(challenge);
            }

            return challenges;
        }

        private ChallengeDto GenerateSmartChallenge(string interest, Guid userId, Random random)
        {
            var (title, targetValue) = interest.ToLower() switch
            {
                "sports" or "fitness" or "exercise" =>
                    ($"Complete {random.Next(5, 10)} workout sessions", random.Next(5000, 15000)),
                "reading" or "books" or "literature" =>
                    ($"Read {random.Next(3, 7)} books this month", random.Next(3, 7)),
                "programming" or "coding" or "development" =>
                    ($"Complete {random.Next(10, 20)} coding exercises", random.Next(10, 20)),
                "music" or "instrument" or "singing" =>
                    ($"Practice {random.Next(30, 60)} minutes daily", random.Next(1800, 3600)),
                "art" or "drawing" or "painting" =>
                    ($"Create {random.Next(5, 10)} artworks", random.Next(5, 10)),
                "writing" or "blogging" =>
                    ($"Write {random.Next(5, 15)} pages", random.Next(5, 15)),
                "cooking" or "baking" =>
                    ($"Try {random.Next(3, 8)} new recipes", random.Next(3, 8)),
                "language" or "linguistics" =>
                    ($"Learn {random.Next(50, 150)} new words", random.Next(50, 150)),
                "photography" =>
                    ($"Take {random.Next(20, 50)} photos", random.Next(20, 50)),
                "meditation" or "mindfulness" =>
                    ($"Meditate for {random.Next(300, 600)} minutes total", random.Next(300, 600)),
                _ =>
                    ($"Master {interest} in {random.Next(7, 30)} days", random.Next(10, 30))
            };

            var description = $"Personalized challenge based on your interest in {interest}. " +
                             "Complete this challenge to earn rewards and improve your skills!";

            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(random.Next(7, 30));

            return new ChallengeDto(
                Guid.NewGuid(),                     // Id будет сгенерирован при сохранении
                startDate,                          // CreatedAt
                startDate,                          // UpdatedAt
                title,
                description,
                GetChallengeType(interest),
                startDate,
                endDate,
                random.Next(0, 2) == 1,            // IsGroupChallenge
                targetValue,
                0,                                  // CurrentValue
                0.0,                               // GroupProgressPercentage
                true,                              // IsActive
                userId,
                new List<ChallengeParticipantDto>());
        }

        private string GetChallengeType(string interest)
        {
            return interest.ToLower() switch
            {
                "sports" or "fitness" => "Exercise",
                "reading" => "Reading",
                "programming" => "Learning",
                "music" => "Practice",
                "art" => "Creative",
                "writing" => "Writing",
                "cooking" => "Culinary",
                "language" => "Education",
                "photography" => "Creative",
                "meditation" => "Wellness",
                _ => "Custom"
            };
        }
    }
}
