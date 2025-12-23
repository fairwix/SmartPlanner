// Tests/Application.UnitTests/Goals/Queries/GetGoalByIdQueryHandlerTests.cs

using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Goals.Queries;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using Xunit;
using GoalCategory = SmartPlanner.Domain.Entities.GoalCategory;

namespace SmartPlanner.Application.UnitTests.Goals.Queries
{
    public class GetGoalByIdQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly GetGoalByIdQueryHandler _handler;
        private readonly Guid _goalId = Guid.NewGuid();
        private readonly Goal _testGoal;

        public GetGoalByIdQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _handler = new GetGoalByIdQueryHandler(_mockContext.Object);

            _testGoal = new Goal
            {
                Id = _goalId,
                Title = "Test Goal",
                Description = "Test Description",
                Category = GoalCategory.Education,
                Priority = GoalPriority.High,
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                CurrentValue = 30,
                IsCompleted = false,
                IsAiGenerated = false,
                RewardAmount = 0,
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
            return mockSet;
        }

        [Fact]
        public async Task Handle_ShouldReturnGoalDto_WhenGoalExists()
        {
            // Arrange
            var goals = new List<Goal> { _testGoal }.AsQueryable();
            var mockDbSet = CreateMockDbSet(goals);

            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            var query = new GetGoalByIdQuery { GoalId = _goalId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(_goalId);
            result.Title.Should().Be("Test Goal");
            result.Description.Should().Be("Test Description");
            result.Category.Should().Be("Education");
            result.Priority.Should().Be("High");
            result.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenGoalDoesNotExist()
        {
            // Arrange
            var goals = new List<Goal>().AsQueryable();
            var mockDbSet = CreateMockDbSet(goals);

            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            var query = new GetGoalByIdQuery { GoalId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldReturnCorrectProgress_WhenGoalHasValues()
        {
            // Arrange
            var goal = new Goal
            {
                Id = _goalId,
                Title = "Progress Test",
                Description = "Test progress calculation",
                Category = GoalCategory.Education,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 200,
                CurrentValue = 150,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = Guid.NewGuid()
            };

            var goals = new List<Goal> { goal }.AsQueryable();
            var mockDbSet = CreateMockDbSet(goals);

            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            var query = new GetGoalByIdQuery { GoalId = _goalId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ShouldReturnCompletedGoal_WhenIsCompletedTrue()
        {
            // Arrange
            var goal = new Goal
            {
                Id = _goalId,
                Title = "Completed Goal",
                Description = "Already completed",
                Category = GoalCategory.Sports,
                Priority = GoalPriority.High,
                DueDate = DateTime.UtcNow.AddDays(-10),
                TargetValue = 100,
                CurrentValue = 100,
                IsCompleted = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                UserId = Guid.NewGuid()
            };

            var goals = new List<Goal> { goal }.AsQueryable();
            var mockDbSet = CreateMockDbSet(goals);

            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            var query = new GetGoalByIdQuery { GoalId = _goalId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.IsCompleted.Should().BeTrue();
            result.IsExpired.Should().BeTrue(); // Due date in past
        }

        [Fact]
        public async Task Handle_ShouldReturnOnTrackGoal_WhenProgressGood()
        {
            // Arrange
            var goal = new Goal
            {
                Id = _goalId,
                Title = "On Track Goal",
                Description = "Good progress",
                Category = GoalCategory.Health,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                CurrentValue = 60, // 60% progress with 50% time passed = on track
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow,
                UserId = Guid.NewGuid()
            };

            var goals = new List<Goal> { goal }.AsQueryable();
            var mockDbSet = CreateMockDbSet(goals);

            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            var query = new GetGoalByIdQuery { GoalId = _goalId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.IsOnTrack.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldReturnAiGeneratedGoal_WhenIsAiGeneratedTrue()
        {
            // Arrange
            var goal = new Goal
            {
                Id = _goalId,
                Title = "AI Generated Goal",
                Description = "Generated by AI",
                Category = GoalCategory.Sports,
                Priority = GoalPriority.Low,
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 50,
                CurrentValue = 10,
                IsCompleted = false,
                IsAiGenerated = true,
                RewardAmount = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = Guid.NewGuid()
            };

            var goals = new List<Goal> { goal }.AsQueryable();
            var mockDbSet = CreateMockDbSet(goals);

            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            var query = new GetGoalByIdQuery { GoalId = _goalId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.IsAiGenerated.Should().BeTrue();
            result.RewardAmount.Should().Be(100);
        }
    }

    // Вспомогательные классы для поддержки async операций
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression)!;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(
                    name: nameof(IQueryProvider.Execute),
                    genericParameterCount: 1,
                    types: new[] { typeof(Expression) })!
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult })!;
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current => _inner.Current;
    }
}
