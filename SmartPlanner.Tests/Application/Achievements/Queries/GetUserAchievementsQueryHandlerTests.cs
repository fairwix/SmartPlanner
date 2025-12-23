// SmartPlanner.Tests/Application/Achievements/Queries/GetUserAchievementsQueryHandlerTests.cs
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
using SmartPlanner.Tests.TestHelpers; // Предполагается наличие MockDbSetHelper
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Queries
{
    public class GetUserAchievementsQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly GetUserAchievementsQueryHandler _handler;
        private readonly Guid _testUserId;

        public GetUserAchievementsQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _handler = new GetUserAchievementsQueryHandler(_mockContext.Object);
            _testUserId = Guid.NewGuid();
        }

        [Fact]
        public async Task Handle_UserHasAchievements_ReturnsMappedDtos()
        {
            // Arrange
            var request = new GetUserAchievementsQuery { UserId = _testUserId };
            var achievement1 = new Achievement { Id = Guid.NewGuid(), Name = "Achievement 1", Description = "Desc 1", BadgeImage = "badge1.png" };
            var achievement2 = new Achievement { Id = Guid.NewGuid(), Name = "Achievement 2", Description = "Desc 2", BadgeImage = "badge2.png" };

            var userAchievement1 = new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                AchievementId = achievement1.Id,
                AwardedAt = DateTime.UtcNow.AddDays(-1),
                Achievement = achievement1 // Подключённое свойство
            };
            var userAchievement2 = new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                AchievementId = achievement2.Id,
                AwardedAt = DateTime.UtcNow,
                Achievement = achievement2 // Подключённое свойство
            };

            var userAchievements = new List<UserAchievement> { userAchievement1, userAchievement2 }.AsQueryable();
            var mockUserAchievements = MockDbSetHelper.CreateMockDbSet(userAchievements.ToList());
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, ua => ua.AchievementName == "Achievement 1" && ua.UserId == _testUserId);
            Assert.Contains(result, ua => ua.AchievementName == "Achievement 2" && ua.UserId == _testUserId);
            Assert.Equal(achievement1.Name, result[0].AchievementName);
            Assert.Equal(achievement1.Description, result[0].AchievementDescription);
            Assert.Equal(achievement1.BadgeImage, result[0].BadgeImage);
            Assert.Equal(achievement2.Name, result[1].AchievementName);
            Assert.Equal(achievement2.Description, result[1].AchievementDescription);
            Assert.Equal(achievement2.BadgeImage, result[1].BadgeImage);
        }

        [Fact]
        public async Task Handle_UserHasNoAchievements_ReturnsEmptyList()
        {
            // Arrange
            var request = new GetUserAchievementsQuery { UserId = _testUserId };

            var emptyUserAchievements = new List<UserAchievement>().AsQueryable();
            var mockUserAchievements = MockDbSetHelper.CreateMockDbSet(emptyUserAchievements.ToList());
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_UserAchievementHasNullAchievement_ReturnsDtoWithEmptyStrings()
        {
            // Arrange
            var request = new GetUserAchievementsQuery { UserId = _testUserId };
            var userAchievementWithNullAchievement = new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                AchievementId = Guid.NewGuid(), // AchievementId есть, но Achievement = null (например, если связь нарушена)
                AwardedAt = DateTime.UtcNow,
                Achievement = null // Подключённое свойство null
            };

            var userAchievements = new List<UserAchievement> { userAchievementWithNullAchievement }.AsQueryable();
            var mockUserAchievements = MockDbSetHelper.CreateMockDbSet(userAchievements.ToList());
            _mockContext.Setup(c => c.UserAchievements).Returns(mockUserAchievements.Object);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0].AchievementName);
            Assert.Equal(string.Empty, result[0].AchievementDescription);
            Assert.Equal(string.Empty, result[0].BadgeImage);
            Assert.Equal(userAchievementWithNullAchievement.AwardedAt, result[0].AwardedAt);
        }
    }
}
