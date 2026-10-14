using FluentValidation;

namespace SmartPlanner.Application.Challenges.Commands;

    public class JoinChallengeCommandValidator : AbstractValidator<JoinChallengeCommand>
    {
        public JoinChallengeCommandValidator()
        {
            RuleFor(x => x.ChallengeId)
                .NotEmpty().WithMessage("Challenge ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
