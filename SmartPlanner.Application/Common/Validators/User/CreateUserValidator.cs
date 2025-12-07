using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.DTOs.User;

namespace SmartPlanner.Application.Common.Validators.User
{
    public class CreateUserValidator : AbstractValidator<CreateUserRequest>
    {
        private readonly IApplicationDbContext _context;

        public CreateUserValidator(IApplicationDbContext context)
        {
            _context = context;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Имя пользователя обязательно")
                .Length(3, 50).WithMessage("Имя пользователя должно быть от 3 до 50 символов")
                .MustAsync(BeUniqueUsername).WithMessage("Пользователь с таким именем уже существует");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен")
                .EmailAddress().WithMessage("Некорректный формат email")
                .MustAsync(BeUniqueEmail).WithMessage("Пользователь с таким email уже существует");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен")
                .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов");
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _context.Users
                .AnyAsync(u => u.Username == username, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Users
                .AnyAsync(u => u.Email == email, cancellationToken);
        }
    }
}
