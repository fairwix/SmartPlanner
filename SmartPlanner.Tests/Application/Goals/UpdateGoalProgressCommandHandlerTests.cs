// SmartPlanner.Tests/Application/Goals/Commands/UpdateGoalProgressCommandHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Services;
using SmartPlanner.Domain.Entities;
using Xunit;
using FluentAssertions;
using SmartPlanner.Application.Interfaces.Services;

namespace SmartPlanner.Tests.Application.Goals.Commands
{
    public class UpdateGoalProgressCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<UpdateGoalProgressCommandHandler>> _mockLogger;
        private readonly UpdateGoalProgressCommandHandler _handler;
        private readonly Guid _testUserId;
        private readonly Guid _testGoalId;

        public UpdateGoalProgressCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<UpdateGoalProgressCommandHandler>>();

            _testUserId = Guid.NewGuid();
            _testGoalId = Guid.NewGuid();

            _handler = new UpdateGoalProgressCommandHandler(
                _mockContext.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ValidProgress_UpdatesGoal()
        {
            // Arrange
            var command = new UpdateGoalProgressCommand
            {
                GoalId = _testGoalId,
                Value = 50
            };

            var goal = new Goal
            {
                Id = _testGoalId,
                UserId = _testUserId,
                TargetValue = 100,
                CurrentValue = 0,
                Title = "Test Goal"
            };

            var mockGoals = MockDbSetHelper.CreateMockDbSet<Goal>(new List<Goal> { goal });
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.CurrentValue.Should().Be(50);
            goal.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_GoalNotFound_ThrowsArgumentException()
        {
            // Arrange
            var command = new UpdateGoalProgressCommand
            {
                GoalId = Guid.NewGuid(), // non-existent goal
                Value = 50
            };

            var mockGoals = MockDbSetHelper.CreateMockDbSet<Goal>(new List<Goal>());
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Goal not found");
        }

        [Fact]
        public async Task Handle_GoalBelongsToAnotherUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var command = new UpdateGoalProgressCommand
            {
                GoalId = _testGoalId,
                Value = 50
            };

            var goal = new Goal
            {
                Id = _testGoalId,
                UserId = Guid.NewGuid(), // different user owns the goal
                TargetValue = 100
            };

            var mockGoals = MockDbSetHelper.CreateMockDbSet<Goal>(new List<Goal> { goal });
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You can only update your own goals");
        }

        [Fact]
        public async Task Handle_ProgressExceedsTarget_ThrowsArgumentException()
        {
            // Arrange
            var command = new UpdateGoalProgressCommand
            {
                GoalId = _testGoalId,
                Value = 150 // exceeds target of 100
            };

            var goal = new Goal
            {
                Id = _testGoalId,
                UserId = _testUserId,
                TargetValue = 100,
                CurrentValue = 0
            };

            var mockGoals = MockDbSetHelper.CreateMockDbSet<Goal>(new List<Goal> { goal });
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Current value cannot exceed target value");
        }

        [Fact]
        public async Task Handle_NegativeProgress_ThrowsArgumentException()
        {
            // Arrange
            var command = new UpdateGoalProgressCommand
            {
                GoalId = _testGoalId,
                Value = -10
            };

            var goal = new Goal
            {
                Id = _testGoalId,
                UserId = _testUserId,
                TargetValue = 100
            };

            var mockGoals = MockDbSetHelper.CreateMockDbSet<Goal>(new List<Goal> { goal });
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Current value cannot be negative");
        }
    }
}
