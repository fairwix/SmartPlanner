// SmartPlanner.Application/Goals/Commands/CreateGoalCommandValidator.cs
using FluentValidation;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Goals.Commands;

    public class CreateGoalCommandValidator : AbstractValidator<CreateGoalCommand>
    {
        public CreateGoalCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Goal title is required")
                .MaximumLength(500).WithMessage("Goal title cannot exceed 500 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required")
                .Must(BeValidCategory).WithMessage("Invalid goal category");

            RuleFor(x => x.Priority)
                .NotEmpty().WithMessage("Priority is required")
                .Must(BeValidPriority).WithMessage("Invalid goal priority");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future");

            RuleFor(x => x.TargetValue)
                .GreaterThan(0).WithMessage("Target value must be positive");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }

        private bool BeValidCategory(string category)
        {
            return Enum.TryParse<GoalCategory>(category, out _);
        }

        private bool BeValidPriority(string priority)
        {
            return Enum.TryParse<GoalPriority>(priority, out _);
        }
    }
