using FluentValidation;
using System.Threading;

using SmartPlanner.Application.DTOs.Goal;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Common.Validators.Goal;

    public class CreateGoalValidator : AbstractValidator<CreateGoalRequest>
    {
        public CreateGoalValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Название цели не может быть пустым")
                .MaximumLength(500).WithMessage("Название цели слишком длинное");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Дата завершения должна быть в будущем");

            RuleFor(x => x.TargetValue)
                .GreaterThan(0).WithMessage("Целевое значение должно быть положительным");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID обязателен");
        }
    }

