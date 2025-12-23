// SmartPlanner.Tests/Application/Goals/Commands/BulkCreateGoalsCommandHandlerTests.cs
using Xunit;
using Moq;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Dtos;
using System;
using SmartPlanner.Tests.TestData;

namespace SmartPlanner.Tests.Application.Goals.Commands
{
    public class BulkCreateGoalsCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<BulkCreateGoalsCommandHandler>> _mockLogger;
        private readonly BulkCreateGoalsCommandHandler _handler;

        public BulkCreateGoalsCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<BulkCreateGoalsCommandHandler>>();

            _handler = new BulkCreateGoalsCommandHandler(
                _mockContext.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsBulkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new BulkCreateGoalsCommand
            {
                UserId = userId,
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto(
                        "Goal 1",
                        "Description 1",
                        "Sports",
                        "High",
                        DateTime.UtcNow.AddDays(7),
                        100,
                        userId
                    ),
                    new CreateGoalDto(
                        "Goal 2",
                        "Description 2",
                        "Education",
                        "Medium",
                        DateTime.UtcNow.AddDays(14),
                        200,
                        userId
                    )
                }
            };

            // Setup user exists
            _mockContext.Setup(c => c.Users.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Setup unique titles
            _mockContext.Setup(c => c.Goals.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Goal, bool>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Setup save changes
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(2); // 2 goals saved

            // Setup mapper
            _mockMapper.Setup(m => m.Map<GoalDto>(It.IsAny<Goal>()))
                .Returns((Goal g) => new GoalDto(
                    g.Id,
                    g.CreatedAt,
                    g.UpdatedAt,
                    g.Title,
                    g.Description,
                    g.Category.ToString(),
                    g.Priority.ToString(),
                    g.DueDate,
                    g.TargetValue,
                    g.CurrentValue,
                    g.GetProgressPercentage(),
                    g.IsCompleted,
                    g.IsAiGenerated,
                    g.RewardAmount,
                    g.UserId,
                    g.IsExpired(),
                    g.IsOnTrack()
                ));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.SuccessfulCount);
            Assert.Equal(0, result.FailedCount);
            Assert.True(result.AllSucceeded);
            Assert.Equal(100.0, result.SuccessRate);

            _mockContext.Verify(c => c.Goals.AddRangeAsync(It.IsAny<List<Goal>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ReturnsAllFailed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new BulkCreateGoalsCommand
            {
                UserId = userId,
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto("Goal 1", "Desc", "Sports", "High", DateTime.UtcNow.AddDays(7), 100, userId),
                    new CreateGoalDto("Goal 2", "Desc", "Education", "Medium", DateTime.UtcNow.AddDays(14), 200, userId)
                }
            };

            // Setup user not exists
            _mockContext.Setup(c => c.Users.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(0, result.SuccessfulCount);
            Assert.Equal(2, result.FailedCount);
            Assert.False(result.AllSucceeded);
            Assert.Equal(0.0, result.SuccessRate);

            _mockContext.Verify(c => c.Goals.AddRangeAsync(It.IsAny<List<Goal>>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DuplicateTitle_ReturnsPartialSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new BulkCreateGoalsCommand
            {
                UserId = userId,
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto("Unique Goal", "Desc", "Sports", "High", DateTime.UtcNow.AddDays(7), 100, userId),
                    new CreateGoalDto("Duplicate Goal", "Desc", "Education", "Medium", DateTime.UtcNow.AddDays(14), 200, userId),
                    new CreateGoalDto("Duplicate Goal", "Desc", "Health", "Low", DateTime.UtcNow.AddDays(21), 300, userId)
                }
            };

            // Setup user exists
            _mockContext.Setup(c => c.Users.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Setup unique check - first goal is unique, second is duplicate
            var callCount = 0;
            _mockContext.Setup(c => c.Goals.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Goal, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => {
                    callCount++;
                    return callCount == 2 || callCount == 3; // Second and third are duplicates
                });

            // Setup save changes
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // Only one goal saved

            // Setup mapper
            _mockMapper.Setup(m => m.Map<GoalDto>(It.IsAny<Goal>()))
                .Returns((Goal g) => new GoalDto(
                    g.Id,
                    g.CreatedAt,
                    g.UpdatedAt,
                    g.Title,
                    g.Description,
                    g.Category.ToString(),
                    g.Priority.ToString(),
                    g.DueDate,
                    g.TargetValue,
                    g.CurrentValue,
                    g.GetProgressPercentage(),
                    g.IsCompleted,
                    g.IsAiGenerated,
                    g.RewardAmount,
                    g.UserId,
                    g.IsExpired(),
                    g.IsOnTrack()
                ));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(1, result.SuccessfulCount);
            Assert.Equal(2, result.FailedCount);
            Assert.False(result.AllSucceeded);
            Assert.Equal(33.33, result.SuccessRate, precision: 2);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
