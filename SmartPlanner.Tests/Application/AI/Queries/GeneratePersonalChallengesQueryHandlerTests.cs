// SmartPlanner.Tests/Application/AI/Queries/GeneratePersonalChallengesQueryHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.AI.Queries;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Tests.TestHelpers; // Предполагается наличие базового класса с MockDbSetHelper
using Xunit;

namespace SmartPlanner.Tests.Application.AI.Queries
{
    public class GeneratePersonalChallengesQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly GeneratePersonalChallengesQueryHandler _handler;

        public GeneratePersonalChallengesQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _handler = new GeneratePersonalChallengesQueryHandler(_mockContext.Object);
        }

        [Fact]
        public async Task Handle_UserExistsWithInterests_GeneratesChallengesBasedOnInterests()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new GeneratePersonalChallengesQuery { UserId = userId, Count = 2 };
            var userWithInterests = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { UserId = userId, Interest = new Interest { Id = Guid.NewGuid(), Name = "Fitness" } },
                    new UserInterest { UserId = userId, Interest = new Interest { Id = Guid.NewGuid(), Name = "Reading" } },
                    // Третий интерес, но Count = 2, так что будет только 2 челленджа
                    new UserInterest { UserId = userId, Interest = new Interest { Id = Guid.NewGuid(), Name = "Programming" } }
                }
            };

            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { userWithInterests });
            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Должно быть сгенерировано 2 челленджа
            Assert.Contains(result, c => c.Title.Contains("workout sessions") || c.Title.Contains("books")); // Проверяем, что были сгенерированы челленджи для Fitness и Reading
            Assert.DoesNotContain(result, c => c.Title.Contains("coding exercises")); // Третий интерес не должен быть использован
        }

        [Fact]
        public async Task Handle_UserExistsWithoutInterests_GeneratesChallengesBasedOnDefaultInterests()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new GeneratePersonalChallengesQuery { UserId = userId, Count = 1 };
            var userWithoutInterests = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                UserInterests = new List<UserInterest>() // Пустой список
            };

            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { userWithoutInterests });
            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Должен быть сгенерирован 1 челлендж
            // Результат может быть любым из дефолтных, проверим, что он не пустой и содержит интерес
            Assert.Contains(result[0].Title, new List<string> { "fitness", "reading", "learning" }, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_UserDoesNotExist_ReturnsEmptyList()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();
            var request = new GeneratePersonalChallengesQuery { UserId = nonExistentUserId, Count = 3 };

            // Настройка мока: возвращаем пустой список пользователей
            var emptyUsers = new List<User>().AsQueryable();
            var mockUsers = MockDbSetHelper.CreateMockDbSet(emptyUsers.ToList());
            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Должен вернуться пустой список
        }

        [Fact]
        public async Task Handle_UserExistsWithInterests_SetsCorrectChallengeProperties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new GeneratePersonalChallengesQuery { UserId = userId, Count = 1 };
            var interestName = "Fitness";
            var userWithInterests = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { UserId = userId, Interest = new Interest { Id = Guid.NewGuid(), Name = interestName } }
                }
            };

            var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { userWithInterests });
            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var challenge = result[0];
            Assert.NotNull(challenge.Id); // Id должен быть сгенерирован
            Assert.Equal(userId, challenge.CreatedBy); // UserId должен быть присвоен
            Assert.Contains(interestName, challenge.Title, StringComparison.OrdinalIgnoreCase); // Название должно отражать интерес
            Assert.Contains(interestName, challenge.Description, StringComparison.OrdinalIgnoreCase); // Описание должно отражать интерес
            Assert.Equal("Exercise", challenge.Type); // Тип должен быть определён на основе интереса
            Assert.True(challenge.IsActive); // IsActive по умолчанию true
            Assert.Equal(0, challenge.CurrentValue); // CurrentValue по умолчанию 0
            Assert.Equal(0.0, challenge.GroupProgressPercentage); // GroupProgressPercentage по умолчанию 0
            Assert.NotNull(challenge.Participants); // Список участников не null
            Assert.Empty(challenge.Participants); // Но пустой
        }
    }
}
