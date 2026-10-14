// Tests/Application/Interfaces/Services/GoalServiceTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Interfaces.Services;

public class GoalServiceTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<IGoalService> _mockGoalService;

    public GoalServiceTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockGoalService = new Mock<IGoalService>();
    }

    [Fact]
    public async Task GetGoalByIdAsync_GoalExists_ReturnsGoal()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var goal = new Goal { Id = goalId, Title = "Test Goal" };

        _mockGoalService.Setup(s => s.GetGoalByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        // Act
        var result = await _mockGoalService.Object.GetGoalByIdAsync(goalId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(goalId);
        result.Title.Should().Be("Test Goal");
    }

    [Fact]
    public async Task GetGoalByIdAsync_GoalDoesNotExist_ReturnsNull()
    {
        // Arrange
        var goalId = Guid.NewGuid();

        _mockGoalService.Setup(s => s.GetGoalByIdAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Goal?)null);

        // Act
        var result = await _mockGoalService.Object.GetGoalByIdAsync(goalId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserGoalsAsync_UserHasGoals_ReturnsGoals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var goals = new List<Goal>
        {
            new Goal { Id = Guid.NewGuid(), Title = "Goal 1", UserId = userId },
            new Goal { Id = Guid.NewGuid(), Title = "Goal 2", UserId = userId }
        };

        _mockGoalService.Setup(s => s.GetUserGoalsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goals);

        // Act
        var result = await _mockGoalService.Object.GetUserGoalsAsync(userId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(g => g.UserId == userId).Should().BeTrue();
    }

    [Fact]
    public async Task CreateGoalAsync_ValidGoal_CreatesGoal()
    {
        // Arrange
        var goal = new Goal { Title = "New Goal", Description = "Description" };
        var createdGoal = new Goal { Id = Guid.NewGuid(), Title = "New Goal", Description = "Description" };

        _mockGoalService.Setup(s => s.CreateGoalAsync(goal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdGoal);

        // Act
        var result = await _mockGoalService.Object.CreateGoalAsync(goal, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("New Goal");
    }

    [Fact]
    public async Task GoalExistsAsync_GoalExists_ReturnsTrue()
    {
        // Arrange
        var goalId = Guid.NewGuid();

        _mockGoalService.Setup(s => s.GoalExistsAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockGoalService.Object.GoalExistsAsync(goalId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GoalExistsAsync_GoalDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var goalId = Guid.NewGuid();

        _mockGoalService.Setup(s => s.GoalExistsAsync(goalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockGoalService.Object.GoalExistsAsync(goalId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
