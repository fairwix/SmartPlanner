// SmartPlanner.Tests/Application/Goals/Commands/CreateGoalCommandHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;
using FluentAssertions;

namespace SmartPlanner.Tests.Application.Goals.Commands
{
    public class CreateGoalCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CreateGoalCommandHandler>> _mockLogger;
        private readonly CreateGoalCommandHandler _handler;
        private readonly Guid _testUserId;

        public CreateGoalCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CreateGoalCommandHandler>>();

            _testUserId = Guid.NewGuid();

            _handler = new CreateGoalCommandHandler(_mockContext.Object, _mockLogger.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task Handle_UserExists_ReturnsGoalDto()
        {
            // Arrange
            SetupMockContextWithUser(true);

            var command = new CreateGoalCommand
            {
                Title = "Test Goal",
                Description = "Test Description",
                Category = "Sports",
                Priority = "High",
                DueDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                UserId = _testUserId
            };

            var expectedGoal = new Goal
            {
                Id = Guid.NewGuid(),
                Title = command.Title,
                Description = command.Description,
                Category = GoalCategory.Sports,
                Priority = GoalPriority.High,
                DueDate = command.DueDate,
                TargetValue = command.TargetValue,
                CurrentValue = 0,
                UserId = command.UserId
            };

            var expectedDto = new GoalDto(
                expectedGoal.Id,
                expectedGoal.CreatedAt,
                expectedGoal.UpdatedAt,
                expectedGoal.Title,
                expectedGoal.Description,
                expectedGoal.Category.ToString(),
                expectedGoal.Priority.ToString(),
                expectedGoal.DueDate,
                expectedGoal.TargetValue,
                expectedGoal.CurrentValue,
                expectedGoal.GetProgressPercentage(),
                expectedGoal.IsCompleted,
                expectedGoal.IsAiGenerated,
                expectedGoal.RewardAmount,
                expectedGoal.UserId,
                expectedGoal.IsExpired(),
                expectedGoal.IsOnTrack()
            );

            _mockMapper.Setup(m => m.Map<GoalDto>(It.IsAny<Goal>())).Returns(expectedDto);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(expectedDto.Title);
            result.Description.Should().Be(expectedDto.Description);
            _mockContext.Verify(c => c.Goals.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsArgumentException()
        {
            // Arrange
            SetupMockContextWithUser(false);

            var command = new CreateGoalCommand
            {
                Title = "Test Goal",
                UserId = _testUserId // non-existent user
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _handler.Handle(command, CancellationToken.None));

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_SetsDefaultValues()
        {
            // Arrange
            SetupMockContextWithUser(true);

            var command = new CreateGoalCommand
            {
                Title = "Test Goal",
                Category = "Sports",
                Priority = "High",
                DueDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                UserId = _testUserId
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var expectedDto = new GoalDto(
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow,
                "Test Goal",
                string.Empty, // Default description
                "Sports",
                "High",
                command.DueDate,
                100,
                0,
                0.0,
                false,
                false,
                10, // Default reward amount
                _testUserId,
                false,
                true
            );

            _mockMapper.Setup(m => m.Map<GoalDto>(It.IsAny<Goal>())).Returns(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Description.Should().Be(string.Empty);
            result.RewardAmount.Should().Be(10);
            result.IsCompleted.Should().BeFalse();
        }

        private void SetupMockContextWithUser(bool userExists)
        {
            var users = userExists
                ? new List<User> { new User { Id = _testUserId } }
                : new List<User>();

            var mockUsers = MockDbSetHelper.CreateMockDbSet(users);
            var mockGoals = MockDbSetHelper.CreateMockDbSet(new List<Goal>());

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);
        }
        // Добавьте этот тест к существующим
        [Fact]
        public async Task Handle_InvalidCategory_ThrowsArgumentException()
        {
            // Arrange
            SetupMockContextWithUser(true);

            var command = new CreateGoalCommand
            {
                Title = "Test Goal",
                Category = "InvalidCategory", // Несуществующая категория
                Priority = "High",
                DueDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                UserId = _testUserId
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }
        // ДОПОЛНИТЕЛЬНЫЕ ТЕСТЫ ДЛЯ СУЩЕСТВУЮЩЕГО ФАЙЛА
[Fact]
public async Task Handle_DuplicateTitle_ThrowsArgumentException()
{
    // Arrange
    SetupMockContextWithUser(true);

    // Add existing goal with same title
    var existingGoal = new Goal
    {
        Id = Guid.NewGuid(),
        Title = "Existing Goal",
        UserId = _testUserId
    };

    var mockGoalsWithExisting = MockDbSetHelper.CreateMockDbSet(new List<Goal> { existingGoal });
    _mockContext.Setup(c => c.Goals).Returns(mockGoalsWithExisting.Object);

    var command = new CreateGoalCommand
    {
        Title = "Existing Goal", // duplicate
        Category = "Sports",
        Priority = "High",
        DueDate = DateTime.UtcNow.AddDays(7),
        TargetValue = 100,
        UserId = _testUserId
    };

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(async () =>
        await _handler.Handle(command, CancellationToken.None));
}

[Fact]
public async Task Handle_InvalidDueDate_ThrowsArgumentException()
{
    // Arrange
    SetupMockContextWithUser(true);

    var command = new CreateGoalCommand
    {
        Title = "Test Goal",
        Category = "Sports",
        Priority = "High",
        DueDate = DateTime.UtcNow.AddDays(-1), // past date
        TargetValue = 100,
        UserId = _testUserId
    };

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(async () =>
        await _handler.Handle(command, CancellationToken.None));
}

[Fact]
public async Task Handle_InvalidTargetValue_ThrowsArgumentException()
{
    // Arrange
    SetupMockContextWithUser(true);

    var command = new CreateGoalCommand
    {
        Title = "Test Goal",
        Category = "Sports",
        Priority = "High",
        DueDate = DateTime.UtcNow.AddDays(7),
        TargetValue = -100, // negative value
        UserId = _testUserId
    };

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(async () =>
        await _handler.Handle(command, CancellationToken.None));
}
    }
}
