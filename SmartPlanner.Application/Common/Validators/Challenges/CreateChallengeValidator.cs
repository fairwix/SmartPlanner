using FluentValidation;
using SmartPlanner.Domain.DTOs.Challenge;
using System.Threading;

namespace SmartPlanner.Domain.Interfaces.Validators.Challenge
{
    public class CreateChallengeValidator : AbstractValidator<CreateChallengeRequest>
    {
        public CreateChallengeValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Название челленджа обязательно")
                .MaximumLength(100).WithMessage("Название челленджа не должно превышать 100 символов");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Описание челленджа не должно превышать 500 символов");

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Дата начала должна быть в будущем");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("Дата окончания должна быть после даты начала");

            RuleFor(x => x.TargetValue)
                .GreaterThan(0).WithMessage("Целевое значение должно быть положительным");
        }
    }
}