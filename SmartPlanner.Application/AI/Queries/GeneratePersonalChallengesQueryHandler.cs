// SmartPlanner.Application/AI/Queries/GeneratePersonalChallengesQueryHandler.cs
using MediatR;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces.Repositories;


namespace SmartPlanner.Application.AI.Queries
{
    public class GeneratePersonalChallengesQueryHandler : IRequestHandler<GeneratePersonalChallengesQuery, List<ChallengeDto>>
    {
        private readonly IUserRepository _userRepository;

        public GeneratePersonalChallengesQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<ChallengeDto>> Handle(GeneratePersonalChallengesQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return new List<ChallengeDto>();

            var challenges = new List<ChallengeDto>();
            var random = new Random();

            // Генерация SMART-челленджей на основе интересов пользователя
            foreach (var interest in user.Interests.Take(request.Count))
            {
                var challenge = GenerateSmartChallenge(interest, user.Id, random);
                challenges.Add(challenge);
            }

            return challenges;
        }

        private ChallengeDto GenerateSmartChallenge(string interest, Guid userId, Random random)
        {
            var (title, description, type, target) = interest.ToLower() switch
            {
                "sports" or "fitness" => (
                    $"Complete {random.Next(3, 7)} workout sessions this week",
                    $"Stay consistent with your fitness goals. Each session should be at least 30 minutes.",
                    "Exercise",
                    random.Next(3, 7)
                ),
                "reading" or "books" => (
                    $"Read {random.Next(2, 5)} books this month",
                    $"Expand your knowledge and build a consistent reading habit.",
                    "Reading",
                    random.Next(2, 5)
                ),
                "programming" or "coding" => (
                    $"Complete {random.Next(5, 15)} coding exercises",
                    $"Improve your programming skills with daily practice.",
                    "Learning",
                    random.Next(5, 15)
                ),
                "music" => (
                    $"Practice {random.Next(10, 30)} minutes daily for a week",
                    $"Develop your musical skills through consistent practice.",
                    "Custom",
                    random.Next(70, 210) // Total minutes per week
                ),
                _ => (
                    $"Master {interest} in {random.Next(7, 30)} days",
                    $"Set specific goals to improve your {interest} skills.",
                    "Custom",
                    random.Next(5, 20)
                )
            };

            return new ChallengeDto
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Type = type,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                IsGroupChallenge = random.Next(0, 2) == 1,
                TargetValue = target,
                CurrentValue = 0,
                GroupProgressPercentage = 0,
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}