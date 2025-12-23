// SmartPlanner.Tests/Application/Achievements/Commands/CheckAndAwardAchievementsCommandHandlerTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Achievements.Commands;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Tests.Application.Achievements.Commands
{
    public class CheckAndAwardAchievementsCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IAchievementCheckerService> _mockAchievementChecker;
        private readonly Mock<IMediator> _mockMediator;
        private readonly CheckAndAwardAchievementsCommandHandler _handler;

        public CheckAndAwardAchievementsCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockAchievementChecker = new Mock<IAchievementCheckerService>();
            _mockMediator = new Mock<IMediator>();

            // ✅ Передаем только 3 параметра
            _handler = new CheckAndAwardAchievementsCommandHandler(
                _mockContext.Object,
                _mockAchievementChecker.Object,
                _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_AwardsAchievements()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new CheckAndAwardAchievementsCommand { UserId = userId };

            var achievement = new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Test Achievement",
                Type = AchievementType.Streak,
                Condition = "streak:7"
            };

            // Настройка мока
            _mockAchievementChecker.Setup(a => a.CheckAndAwardEligibleAchievementsAsync(userId, _mockContext.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Achievement> { achievement });

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockMediator.Verify(m => m.Send(It.Is<AwardAchievementCommand>(c => c.UserId == userId && c.AchievementId == achievement.Id), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
