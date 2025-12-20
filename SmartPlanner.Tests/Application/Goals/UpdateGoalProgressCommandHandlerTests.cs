using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Domain.Entities;
using Xunit;

using Microsoft.EntityFrameworkCore;

namespace SmartPlanner.Tests.Application.Goals
{
    public class UpdateGoalProgressCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<UpdateGoalProgressCommandHandler>> _mockLogger;
        private readonly UpdateGoalProgressCommandHandler _handler;

        public UpdateGoalProgressCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<UpdateGoalProgressCommandHandler>>();
            _handler = new UpdateGoalProgressCommandHandler(_mockContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_GoalNotFound_ShouldReturnNull()
        {
            // Arrange
            var command = new UpdateGoalProgressCommand
            {
                GoalId = Guid.NewGuid(),
                Value = 50
            };

            var mockGoals = Helpers.MockDbSetHelper.CreateMockDbSet<Goal>(new List<Goal>());
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Goal with ID")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidProgressUpdate_ShouldUpdateGoalAndReturnDto()
        {
            // Arrange
            var goalId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Balance = 100,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var goal = new Goal
            {
                Id = goalId,
                Title = "Test Goal",
                TargetValue = 100,
                CurrentValue = 30,
                UserId = userId,
                User = user,
                RewardAmount = 50,
                Category = GoalCategory.Sports,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsAiGenerated = false,
                IsCompleted = false,
                Description = "Test Description"
            };

            var command = new UpdateGoalProgressCommand
            {
                GoalId = goalId,
                Value = 80
            };

            var mockGoals = Helpers.MockDbSetHelper.CreateMockDbSet(new List<Goal> { goal });

            // Настраиваем Include
            mockGoals.Setup(m => m.Include(It.IsAny<string>()))
                .Returns(mockGoals.Object)
                .Verifiable();

            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentValue.Should().Be(80);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
