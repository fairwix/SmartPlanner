// Tests/Application.UnitTests/Goals/Queries/GetUserGoalsQueryHandlerTests.cs

using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Goals.Queries;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using Xunit;
using GoalCategory = SmartPlanner.Domain.Entities.GoalCategory;

namespace SmartPlanner.Application.UnitTests.Goals.Queries
{
    public class GetUserGoalsQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<GetUserGoalsQueryHandler>> _mockLogger;
        private readonly GetUserGoalsQueryHandler _handler;
        private readonly List<Goal> _goals;
        private readonly Guid _userId = Guid.NewGuid();

        public GetUserGoalsQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<GetUserGoalsQueryHandler>>();

            _handler = new GetUserGoalsQueryHandler(
                _mockContext.Object,
                _mockMapper.Object,
                _mockLogger.Object);

            // Подготовка тестовых данных
            _goals = new List<Goal>
            {
                new Goal
                {
                    Id = Guid.NewGuid(),
                    UserId = _userId,
                    Title = "Учить C#",
                    Description = "Изучить продвинутые фичи C#",
                    Category = GoalCategory.Education,
                    Priority = GoalPriority.High,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    TargetValue = 100,
                    CurrentValue = 30,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    UserId = _userId,
                    Title = "Тренировка",
                    Description = "Тренироваться 3 раза в неделю",
                    Category = GoalCategory.Health,
                    Priority = GoalPriority.Medium,
                    DueDate = DateTime.UtcNow.AddDays(60),
                    TargetValue = 36,
                    CurrentValue = 12,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    UserId = _userId,
                    Title = "Прочитать книгу",
                    Description = "Прочитать 'Чистый код'",
                    Category = GoalCategory.Education,
                    Priority = GoalPriority.Low,
                    DueDate = DateTime.UtcNow.AddDays(90),
                    TargetValue = 1,
                    CurrentValue = 0,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    UserId = _userId,
                    Title = "Завершенная цель",
                    Description = "Уже выполнено",
                    Category = GoalCategory.Sports,
                    Priority = GoalPriority.High,
                    DueDate = DateTime.UtcNow.AddDays(-10),
                    TargetValue = 100,
                    CurrentValue = 100,
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                }
            };

            var mockDbSet = CreateMockDbSet(_goals.AsQueryable());
            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            return mockSet;
        }

        [Fact]
        public async Task Handle_ShouldReturnPagedGoals_WhenValidQuery()
        {
            // Arrange
            var query = new GetUserGoalsQuery
            {
                UserId = _userId,
                PageNumber = 1,
                PageSize = 2
            };

            var expectedDtos = new List<GoalDto>
            {
                new GoalDto(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "Title", "Desc",
                    "Education", "High", DateTime.UtcNow, 100, 30, 30, false, false, 0, _userId, false, true),
                new GoalDto(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "Title2", "Desc2",
                    "Health", "Medium", DateTime.UtcNow, 36, 12, 33, false, false, 0, _userId, false, true)
            };

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns(expectedDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(4);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task Handle_ShouldFilterByCategory_WhenCategoryProvided()
        {
            // Arrange
            var query = new GetUserGoalsQuery
            {
                UserId = _userId,
                Category = "Education",
                PageNumber = 1,
                PageSize = 10
            };

            var filteredGoals = _goals.Where(g => g.Category == GoalCategory.Education).ToList();
            var mockDbSet = CreateMockDbSet(_goals.AsQueryable());
            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns((List<Goal> goals) => goals.Select(g => new GoalDto(
                    g.Id, g.CreatedAt, g.UpdatedAt, g.Title, g.Description,
                    g.Category.ToString(), g.Priority.ToString(), g.DueDate,
                    g.TargetValue, g.CurrentValue, g.GetProgressPercentage(),
                    g.IsCompleted, g.IsAiGenerated, g.RewardAmount, g.UserId,
                    g.IsExpired(), g.IsOnTrack())).ToList());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2); // 2 goals in Education category
        }

        [Fact]
        public async Task Handle_ShouldFilterByPriority_WhenPriorityProvided()
        {
            // Arrange
            var query = new GetUserGoalsQuery
            {
                UserId = _userId,
                Priority = "High",
                PageNumber = 1,
                PageSize = 10
            };

            var filteredGoals = _goals.Where(g => g.Priority == GoalPriority.High).ToList();
            var mockDbSet = CreateMockDbSet(_goals.AsQueryable());
            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns((List<Goal> goals) => goals.Select(g => new GoalDto(
                    g.Id, g.CreatedAt, g.UpdatedAt, g.Title, g.Description,
                    g.Category.ToString(), g.Priority.ToString(), g.DueDate,
                    g.TargetValue, g.CurrentValue, g.GetProgressPercentage(),
                    g.IsCompleted, g.IsAiGenerated, g.RewardAmount, g.UserId,
                    g.IsExpired(), g.IsOnTrack())).ToList());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2); // 2 goals with High priority
        }

        [Fact]
        public async Task Handle_ShouldFilterByCompleted_WhenCompletedProvided()
        {
            // Arrange
            var query = new GetUserGoalsQuery
            {
                UserId = _userId,
                Completed = true,
                PageNumber = 1,
                PageSize = 10
            };

            var filteredGoals = _goals.Where(g => g.IsCompleted).ToList();
            var mockDbSet = CreateMockDbSet(_goals.AsQueryable());
            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns((List<Goal> goals) => goals.Select(g => new GoalDto(
                    g.Id, g.CreatedAt, g.UpdatedAt, g.Title, g.Description,
                    g.Category.ToString(), g.Priority.ToString(), g.DueDate,
                    g.TargetValue, g.CurrentValue, g.GetProgressPercentage(),
                    g.IsCompleted, g.IsAiGenerated, g.RewardAmount, g.UserId,
                    g.IsExpired(), g.IsOnTrack())).ToList());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1); // 1 completed goal
        }

        [Fact]
        public async Task Handle_ShouldSearchByText_WhenSearchProvided()
        {
            // Arrange
            var query = new GetUserGoalsQuery
            {
                UserId = _userId,
                Search = "C#",
                PageNumber = 1,
                PageSize = 10
            };

            var mockDbSet = CreateMockDbSet(_goals.AsQueryable());
            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns((List<Goal> goals) => goals.Select(g => new GoalDto(
                    g.Id, g.CreatedAt, g.UpdatedAt, g.Title, g.Description,
                    g.Category.ToString(), g.Priority.ToString(), g.DueDate,
                    g.TargetValue, g.CurrentValue, g.GetProgressPercentage(),
                    g.IsCompleted, g.IsAiGenerated, g.RewardAmount, g.UserId,
                    g.IsExpired(), g.IsOnTrack())).ToList());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1); // 1 goal containing "C#"
        }

        [Theory]
        [InlineData("title", "asc")]
        [InlineData("duedate", "desc")]
        [InlineData("priority", "asc")]
        [InlineData("category", "desc")]
        [InlineData("createdat", "asc")]
        [InlineData("progress", "desc")]
        public async Task Handle_ShouldApplySorting_WhenSortByProvided(string sortBy, string sortOrder)
        {
            // Arrange
            var query = new GetUserGoalsQuery
            {
                UserId = _userId,
                SortBy = sortBy,
                SortOrder = sortOrder,
                PageNumber = 1,
                PageSize = 10
            };

            var mockDbSet = CreateMockDbSet(_goals.AsQueryable());
            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns((List<Goal> goals) => goals.Select(g => new GoalDto(
                    g.Id, g.CreatedAt, g.UpdatedAt, g.Title, g.Description,
                    g.Category.ToString(), g.Priority.ToString(), g.DueDate,
                    g.TargetValue, g.CurrentValue, g.GetProgressPercentage(),
                    g.IsCompleted, g.IsAiGenerated, g.RewardAmount, g.UserId,
                    g.IsExpired(), g.IsOnTrack())).ToList());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(4);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmpty_WhenNoGoalsForUser()
        {
            // Arrange
            var query = new GetUserGoalsQuery
            {
                UserId = Guid.NewGuid(), // Different user
                PageNumber = 1,
                PageSize = 10
            };

            var mockDbSet = CreateMockDbSet(_goals.AsQueryable());
            _mockContext.Setup(c => c.Goals).Returns(mockDbSet.Object);

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns(new List<GoalDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task HandleAdvanced_ShouldReturnPagedGoals_WhenValidQuery()
        {
            // Arrange
            var query = new GetUserGoalsAdvancedQuery
            {
                UserId = _userId,
                Pagination = new AdvancedPaginationRequest
                {
                    PageNumber = 1,
                    PageSize = 2
                }
            };

            var expectedDtos = new List<GoalDto>
            {
                new GoalDto(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "Title", "Desc",
                    "Education", "High", DateTime.UtcNow, 100, 30, 30, false, false, 0, _userId, false, true)
            };

            _mockMapper.Setup(m => m.Map<List<GoalDto>>(It.IsAny<List<Goal>>()))
                .Returns(expectedDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
        }
    }
}
