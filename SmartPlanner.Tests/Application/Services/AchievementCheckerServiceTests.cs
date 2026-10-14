// Tests/Application/Services/AchievementCheckerServiceTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Services;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Services;

public class AchievementCheckerServiceTests
{
    private readonly AchievementCheckerService _service;
    private readonly Mock<IApplicationDbContext> _mockContext;

    public AchievementCheckerServiceTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _service = new AchievementCheckerService();
    }

    [Fact]
    public async Task CheckAndAwardEligibleAchievementsAsync_UserNotFound_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockUsersSet = new Mock<DbSet<User>>();
        var users = new List<User>().AsQueryable();

        mockUsersSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
        mockUsersSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
        mockUsersSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
        mockUsersSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());

        _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);

        // Act
        var result = await _service.CheckAndAwardEligibleAchievementsAsync(
            userId, _mockContext.Object, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAndAwardEligibleAchievementsAsync_UserHasNoAchievements_ReturnsAllEligible()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            StreakCount = 10
        };

        var achievements = new List<Achievement>
        {
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "7-Day Streak",
                Type = AchievementType.Streak,
                Condition = "streak:7",
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "30-Day Streak",
                Type = AchievementType.Streak,
                Condition = "streak:30",
            }
        };

        var mockUsersSet = CreateMockDbSet(new List<User> { user });
        var mockAchievementsSet = CreateMockDbSet(achievements);

        _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
        _mockContext.Setup(c => c.Achievements).Returns(mockAchievementsSet.Object);

        // Setup для Include и Select
        var queryable = new List<object>
        {
            new
            {
                User = user,
                CompletedGoalsCount = 5,
                FriendsCount = 3,
                UserAchievementIds = new List<Guid>()
            }
        }.AsQueryable();

        var mockQueryableSet = new Mock<DbSet<object>>();
        mockQueryableSet.As<IQueryable<object>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockQueryableSet.As<IQueryable<object>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockQueryableSet.As<IQueryable<object>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockQueryableSet.As<IQueryable<object>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        // Act
        var result = await _service.CheckAndAwardEligibleAchievementsAsync(
            userId, _mockContext.Object, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1); // Only 7-Day Streak eligible (streak 10 >= 7)
        result[0].Name.Should().Be("7-Day Streak");
    }

    [Fact]
    public async Task CheckAndAwardEligibleAchievementsAsync_UserAlreadyHasAchievement_SkipsIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            StreakCount = 10
        };

        var achievements = new List<Achievement>
        {
            new Achievement
            {
                Id = achievementId,
                Name = "7-Day Streak",
                Type = AchievementType.Streak,
                Condition = "streak:7",
            }
        };

        var queryable = new List<object>
        {
            new
            {
                User = user,
                CompletedGoalsCount = 0,
                FriendsCount = 0,
                UserAchievementIds = new List<Guid> { achievementId } // Already has this achievement
            }
        }.AsQueryable();

        var mockUsersSet = CreateMockDbSet(new List<User> { user });
        var mockAchievementsSet = CreateMockDbSet(achievements);

        _mockContext.Setup(c => c.Users).Returns(mockUsersSet.Object);
        _mockContext.Setup(c => c.Achievements).Returns(mockAchievementsSet.Object);

        // Act
        var result = await _service.CheckAndAwardEligibleAchievementsAsync(
            userId, _mockContext.Object, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Should skip because already has it
    }

    [Theory]
    [InlineData(10, "streak:7", true)] // Streak 10 >= 7
    [InlineData(5, "streak:7", false)] // Streak 5 < 7
    [InlineData(0, "streak:7", false)] // Streak 0 < 7
    [InlineData(15, "streak:15", true)] // Streak 15 >= 15
    public void MeetsAchievementCondition_StreakAchievement_ReturnsCorrectResult(int streakCount, string condition, bool expected)
    {
        // Arrange
        var achievement = new Achievement
        {
            Type = AchievementType.Streak,
            Condition = condition
        };

        var user = new User { StreakCount = streakCount };

        // Act
        var result = _service.MeetsAchievementCondition(achievement, user, 0, 0);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, "goals_completed:5", true)] // 10 goals >= 5 required
    [InlineData(3, "goals_completed:5", false)] // 3 goals < 5 required
    [InlineData(0, "goals_completed:1", false)] // 0 goals < 1 required
    [InlineData(100, "goals_completed:100", true)] // 100 goals >= 100 required
    public void MeetsAchievementCondition_GoalsCompletedAchievement_ReturnsCorrectResult(int completedGoals, string condition, bool expected)
    {
        // Arrange
        var achievement = new Achievement
        {
            Type = AchievementType.GoalsCompleted,
            Condition = condition
        };

        var user = new User();

        // Act
        var result = _service.MeetsAchievementCondition(achievement, user, completedGoals, 0);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, "friends:5", true)] // 10 friends >= 5 required
    [InlineData(3, "friends:5", false)] // 3 friends < 5 required
    [InlineData(0, "friends:1", false)] // 0 friends < 1 required
    [InlineData(50, "friends:50", true)] // 50 friends >= 50 required
    public void MeetsAchievementCondition_FriendsAchievement_ReturnsCorrectResult(int friendsCount, string condition, bool expected)
    {
        // Arrange
        var achievement = new Achievement
        {
            Type = AchievementType.Friends,
            Condition = condition
        };

        var user = new User();

        // Act
        var result = _service.MeetsAchievementCondition(achievement, user, 0, friendsCount);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void MeetsAchievementCondition_InvalidConditionFormat_ReturnsFalse()
    {
        // Arrange
        var achievement = new Achievement
        {
            Type = AchievementType.Streak,
            Condition = "invalid:format" // Invalid format
        };

        var user = new User { StreakCount = 10 };

        // Act
        var result = _service.MeetsAchievementCondition(achievement, user, 0, 0);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MeetsAchievementCondition_UnknownAchievementType_ReturnsFalse()
    {
        // Arrange
        var achievement = new Achievement
        {
            Type = (AchievementType)999, // Unknown type
            Condition = "streak:7"
        };

        var user = new User { StreakCount = 10 };

        // Act
        var result = _service.MeetsAchievementCondition(achievement, user, 0, 0);

        // Assert
        result.Should().BeFalse();
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        return mockSet;
    }
}
