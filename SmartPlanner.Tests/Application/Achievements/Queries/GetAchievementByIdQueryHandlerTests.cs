// SmartPlanner.Tests/Application/Achievements/Queries/GetAchievementByIdQueryHandlerTests.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
    public class GetAchievementByIdQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper; // AutoMapper не используется в этом конкретном хендлере, но пусть будет в конструкторе
        private readonly GetAchievementByIdQueryHandler _handler;

        public GetAchievementByIdQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMapper = new Mock<IMapper>();
            _handler = new GetAchievementByIdQueryHandler(_mockContext.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task Handle_AchievementExists_ReturnsMappedDto()
        {
            // Arrange
            var achievementId = Guid.NewGuid();
            var request = new GetAchievementByIdQuery { AchievementId = achievementId };
            var achievement = new Achievement
            {
                Id = achievementId,
                Name = "Test Achievement",
                Description = "A test achievement",
                BadgeImage = "badge.png",
                RewardAmount = 100,
                Type = AchievementType.Streak,
                Condition = "streak:7",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var mockAchievements = MockDbSetHelper.CreateMockDbSet(new List<Achievement> { achievement });
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(achievement.Name, result.Name);
            Assert.Equal(achievement.Description, result.Description);
            Assert.Equal(achievement.BadgeImage, result.BadgeImage);
            Assert.Equal(achievement.RewardAmount, result.RewardAmount);
            Assert.Equal(achievement.Type.ToString(), result.Type); // Проверяем, что enum конвертируется в строку
            Assert.Equal(achievement.Condition, result.Condition);
        }

        [Fact]
        public async Task Handle_AchievementDoesNotExist_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var request = new GetAchievementByIdQuery { AchievementId = nonExistentId };

            var emptyAchievements = new List<Achievement>().AsQueryable();
            var mockAchievements = MockDbSetHelper.CreateMockDbSet(emptyAchievements.ToList());
            _mockContext.Setup(c => c.Achievements).Returns(mockAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
