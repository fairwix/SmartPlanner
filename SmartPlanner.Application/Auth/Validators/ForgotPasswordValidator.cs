using FluentValidation;
using SmartPlanner.Application.Auth.Dtos;

namespace SmartPlanner.Application.Auth.Validators;

public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email address is required")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters");
    }
}
