using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Auth.Dtos;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Auth.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        private readonly IApplicationDbContext _context;

        public RegisterDtoValidator(IApplicationDbContext context)
        {
            _context = context;

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Valid email address is required")
                .MaximumLength(200).WithMessage("Email cannot exceed 200 characters")
                .MustAsync(BeUniqueEmail).WithMessage("Email is already registered");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores")
                .MustAsync(BeUniqueUsername).WithMessage("Username is already taken");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

            RuleFor(x => x.DateOfBirth)
                .Must(BeValidDateOfBirth).WithMessage("You must be at least 18 years old")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
        }

        private bool BeValidDateOfBirth(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return true;
            var age = DateTime.UtcNow.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value.Date > DateTime.UtcNow.AddYears(-age)) age--;
            return age >= 18;
        }
    }
}
