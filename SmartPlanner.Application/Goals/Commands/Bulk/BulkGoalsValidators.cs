// SmartPlanner.Application/Goals/Commands/BulkGoalsValidators.cs
using FluentValidation;
using SmartPlanner.Application.Goals.Commands;

namespace SmartPlanner.Application.Goals.Commands;

    public class BulkCreateGoalsCommandValidator : AbstractValidator<BulkCreateGoalsCommand>
    {
        public BulkCreateGoalsCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Goals)
                .NotEmpty().WithMessage("At least one goal is required for bulk creation")
                .Must(goals => goals.Count <= 100).WithMessage("Cannot create more than 100 goals at once");

            RuleForEach(x => x.Goals)
                .ChildRules(goal =>
                {
                    goal.RuleFor(g => g.Title)
                        .NotEmpty().WithMessage("Goal title is required")
                        .MaximumLength(500).WithMessage("Goal title cannot exceed 500 characters");

                    goal.RuleFor(g => g.DueDate)
                        .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future");

                    goal.RuleFor(g => g.TargetValue)
                        .GreaterThan(0).WithMessage("Target value must be positive");
                });
        }
    }

    public class BulkUpdateGoalsCommandValidator : AbstractValidator<BulkUpdateGoalsCommand>
    {
        public BulkUpdateGoalsCommandValidator()
        {
            RuleFor(x => x.Goals)
                .NotEmpty().WithMessage("At least one goal update is required")
                .Must(goals => goals.Count <= 50).WithMessage("Cannot update more than 50 goals at once");

            RuleForEach(x => x.Goals)
                .ChildRules(updateItem =>
                {
                    updateItem.RuleFor(i => i.GoalId)
                        .NotEmpty().WithMessage("Goal ID is required");

                    updateItem.RuleFor(i => i.UpdateData)
                        .NotNull().WithMessage("Update data is required");
                });
        }
    }

    public class BulkDeleteGoalsCommandValidator : AbstractValidator<BulkDeleteGoalsCommand>
    {
        public BulkDeleteGoalsCommandValidator()
        {
            RuleFor(x => x.GoalIds)
                .NotEmpty().WithMessage("At least one goal ID is required for bulk deletion")
                .Must(ids => ids.Count <= 100).WithMessage("Cannot delete more than 100 goals at once")
                .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("All goal IDs must be valid");
        }
    }
