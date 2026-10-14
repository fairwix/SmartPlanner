// Tests/Unit/Application/Validators/User/CreateUserValidatorTests.cs
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.DTOs.User;
using SmartPlanner.Application.Common.Validators.User;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Validators.User
{
    public class CreateUserValidatorTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly CreateUserValidator _validator;

        public CreateUserValidatorTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _validator = new CreateUserValidator(_contextMock.Object);
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return mockSet;
        }

        [Fact]
        public void Should_HaveError_WhenUsernameEmpty()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = string.Empty,
                Email = "test@example.com",
                Password = "password123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Имя пользователя обязательно");
        }

        [Fact]
        public void Should_HaveError_WhenUsernameTooShort()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "ab", // 2 characters
                Email = "test@example.com",
                Password = "password123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Имя пользователя должно быть от 3 до 50 символов");
        }

        [Fact]
        public void Should_HaveError_WhenUsernameTooLong()
        {
            // Arrange
            var longUsername = new string('A', 51); // 51 characters
            var request = new CreateUserRequest
            {
                Username = longUsername,
                Email = "test@example.com",
                Password = "password123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Имя пользователя должно быть от 3 до 50 символов");
        }

        [Fact]
        public async Task Should_HaveError_WhenUsernameAlreadyExists()
        {
            // Arrange
            var existingUsername = "existinguser";
            var users = new List<Domain.Entities.User>
            {
                new Domain.Entities.User { Id = Guid.NewGuid(), Username = existingUsername, Email = "existing@example.com" }
            };

            var mockSet = CreateMockDbSet(users);
            _contextMock.Setup(c => c.Users).Returns(mockSet.Object);

            var request = new CreateUserRequest
            {
                Username = existingUsername,
                Email = "new@example.com",
                Password = "password123"
            };

            // Act
            var result = await _validator.TestValidateAsync(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Пользователь с таким именем уже существует");
        }

        [Fact]
        public void Should_HaveError_WhenEmailEmpty()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "testuser",
                Email = string.Empty,
                Password = "password123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email обязателен");
        }

        [Fact]
        public void Should_HaveError_WhenEmailInvalidFormat()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "testuser",
                Email = "not-an-email",
                Password = "password123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Некорректный формат email");
        }

        [Fact]
        public async Task Should_HaveError_WhenEmailAlreadyExists()
        {
            // Arrange
            var existingEmail = "existing@example.com";
            var users = new List<Domain.Entities.User>
            {
                new Domain.Entities.User { Id = Guid.NewGuid(), Username = "user1", Email = existingEmail }
            };

            var mockSet = CreateMockDbSet(users);
            _contextMock.Setup(c => c.Users).Returns(mockSet.Object);

            var request = new CreateUserRequest
            {
                Username = "newuser",
                Email = existingEmail,
                Password = "password123"
            };

            // Act
            var result = await _validator.TestValidateAsync(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Пользователь с таким email уже существует");
        }

        [Fact]
        public void Should_HaveError_WhenPasswordEmpty()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = string.Empty
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Пароль обязателен");
        }

        [Fact]
        public void Should_HaveError_WhenPasswordTooShort()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "12345" // 5 characters
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Пароль должен содержать минимум 6 символов");
        }

        [Fact]
        public async Task Should_BeValid_WhenAllFieldsCorrectAndUnique()
        {
            // Arrange
            var mockSet = CreateMockDbSet(new List<Domain.Entities.User>()); // Empty database
            _contextMock.Setup(c => c.Users).Returns(mockSet.Object);

            var request = new CreateUserRequest
            {
                Username = "newuser",
                Email = "new@example.com",
                Password = "password123",
                Interests = new List<string> { "Sports", "Reading" }
            };

            // Act
            var result = await _validator.TestValidateAsync(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_NotValidateInterests()
        {
            // Arrange - Interests can be null or empty
            var request1 = new CreateUserRequest
            {
                Username = "user1",
                Email = "test1@example.com",
                Password = "password123",
                Interests = null!
            };

            var request2 = new CreateUserRequest
            {
                Username = "user2",
                Email = "test2@example.com",
                Password = "password123",
                Interests = new List<string>() // Empty list
            };

            var request3 = new CreateUserRequest
            {
                Username = "user3",
                Email = "test3@example.com",
                Password = "password123",
                Interests = new List<string> { "Music", "Art" }
            };

            // Act & Assert - No validation errors for Interests
            _validator.TestValidate(request1).ShouldNotHaveValidationErrorFor(x => x.Interests);
            _validator.TestValidate(request2).ShouldNotHaveValidationErrorFor(x => x.Interests);
            _validator.TestValidate(request3).ShouldNotHaveValidationErrorFor(x => x.Interests);
        }

        [Fact]
        public async Task Should_AllowValidEmailFormats()
        {
            // Arrange
            var mockSet = CreateMockDbSet(new List<Domain.Entities.User>());
            _contextMock.Setup(c => c.Users).Returns(mockSet.Object);

            var validEmails = new[]
            {
                "user@example.com",
                "user.name@example.com",
                "user+tag@example.com",
                "user@sub.example.com",
                "user@example.co.uk"
            };

            foreach (var email in validEmails)
            {
                var request = new CreateUserRequest
                {
                    Username = $"user{Guid.NewGuid():N}",
                    Email = email,
                    Password = "password123"
                };

                // Act
                var result = await _validator.TestValidateAsync(request);

                // Assert
                result.ShouldNotHaveValidationErrorFor(x => x.Email);
            }
        }
    }
}
