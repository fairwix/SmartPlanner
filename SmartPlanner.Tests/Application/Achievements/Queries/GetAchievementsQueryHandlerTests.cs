// SmartPlanner.Tests/Application/Achievements/Queries/GetAchievementsQueryHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Achievements.Queries;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using SmartPlanner.Tests.TestHelpers; // Предполагается наличие MockDbSetHelper
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Queries
{
    public class GetAchievementsQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly GetAchievementsQueryHandler _handler;

        public GetAchievementsQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _handler = new GetAchievementsQueryHandler(_mockContext.Object);
        }

        [Fact]
        public async Task Handle_NoTypeFilter_ReturnsAllAchievements()
        {
            // Arrange
            var request = new GetAchievementsQuery();
            var achievements = new List<Achievement>
            {
                new Achievement { Id = Guid.NewGuid(), Name = "Achievement 1", Type = AchievementType.Streak },
                new Achievement { Id = Guid.NewGuid(), Name = "Goals Achievement", Type = (AchievementType)Enum.Parse(typeof(AchievementType), "CompletedGoals") }, // Если AchievementType существует, но не виден
                new Achievement { Id = Guid.NewGuid(), Name = "Achievement 3", Type = (AchievementType)Enum.Parse(typeof(AchievementType), "FriendsCount") }
            }.AsQueryable();

            var mockAchievements = MockDbSetHelper.CreateMockDbSet(achievements.ToList());
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, a => a.Name == "Achievement 1");
            Assert.Contains(result, a => a.Name == "Achievement 2");
            Assert.Contains(result, a => a.Name == "Achievement 3");
        }

        [Fact]
        public async Task Handle_ValidTypeFilter_ReturnsFilteredAchievements()
        {
            // Arrange
            var request = new GetAchievementsQuery { AchievementType = "Streak" };
            var achievements = new List<Achievement>
            {
                new Achievement { Id = Guid.NewGuid(), Name = "Streak Achievement", Type = AchievementType.Streak },
                new Achievement { Id = Guid.NewGuid(), Name = "Goals Achievement", Type = (AchievementType)Enum.Parse(typeof(AchievementType), "CompletedGoals") }, // Если AchievementType существует, но не виден
                new Achievement { Id = Guid.NewGuid(), Name = "Friends Achievement", Type = (AchievementType)Enum.Parse(typeof(AchievementType), "FriendsCount") }  // Не подходит
            }.AsQueryable();

            var mockAchievements = MockDbSetHelper.CreateMockDbSet(achievements.ToList());
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Только 1 достижение типа Streak
            Assert.Equal("Streak Achievement", result[0].Name);
            Assert.Equal("Streak", result[0].Type);
        }

        [Fact]
        public async Task Handle_InvalidTypeFilter_ReturnsEmptyList()
        {
            // Arrange
            var request = new GetAchievementsQuery { AchievementType = "InvalidType" }; // Несуществующий тип
            var achievements = new List<Achievement>
            {
                new Achievement { Id = Guid.NewGuid(), Name = "Streak Achievement", Type = AchievementType.Streak }
            }.AsQueryable();

            var mockAchievements = MockDbSetHelper.CreateMockDbSet(achievements.ToList());
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Парсер не нашёл тип, фильтр не сработал, но возвратили всё равно список (пустой в данном случае, так как фильтр сработал как `.Where(a => a.Type == null)` или не сработал вовсе, в зависимости от реализации Enum.TryParse). В данном случае, `.Where` вернёт пустой результат, если `type` не инициализирован.
        }

        [Fact]
        public async Task Handle_EmptyTypeFilter_ReturnsAllAchievements()
        {
            // Arrange
            var request = new GetAchievementsQuery { AchievementType = "" }; // Пустая строка
            var achievements = new List<Achievement>
            {
                new Achievement { Id = Guid.NewGuid(), Name = "Achievement 1", Type = AchievementType.Streak },
                new Achievement { Id = Guid.NewGuid(), Name = "Achievement 2", Type = (AchievementType)Enum.Parse(typeof(AchievementType), "CompletedGoals") }, // Если AchievementType существует, но не виден
            }.AsQueryable();

            var mockAchievements = MockDbSetHelper.CreateMockDbSet(achievements.ToList());
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Возвращаются все, так как фильтр не применяется
        }
    }
}
