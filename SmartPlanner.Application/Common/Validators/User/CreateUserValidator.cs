using FluentValidation;
using System.Threading;
using SmartPlanner.Application.DTOs.User;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Common.Validators.User;

    public class CreateUserValidator : AbstractValidator<CreateUserRequest>
    {
        private readonly IUserRepository _userRepository;

        public CreateUserValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;

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
                .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов")
                .Matches("[A-Z]").WithMessage("Пароль должен содержать хотя бы одну заглавную букву")
                .Matches("[a-z]").WithMessage("Пароль должен содержать хотя бы одну строчную букву")
                .Matches("[0-9]").WithMessage("Пароль должен содержать хотя бы одну цифру");
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _userRepository.ExistsByUsernameAsync(username, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _userRepository.ExistsByEmailAsync(email, cancellationToken);
        }
    }

