using FluentValidation;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Challenges.Commands
{
    public class CreateChallengeCommandValidator : AbstractValidator<CreateChallengeCommand>
    {
        public CreateChallengeCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Challenge title is required")
                .MaximumLength(100).WithMessage("Title cannot exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Challenge type is required")
                .Must(BeValidChallengeType).WithMessage("Invalid challenge type");

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

            RuleFor(x => x.TargetValue)
                .GreaterThan(0).WithMessage("Target value must be positive");

            RuleFor(x => x.CreatedBy)
                .NotEmpty().WithMessage("Creator ID is required");
        }

        private bool BeValidChallengeType(string type)
        {
            return Enum.TryParse<ChallengeType>(type, out _);
        }
    }
}