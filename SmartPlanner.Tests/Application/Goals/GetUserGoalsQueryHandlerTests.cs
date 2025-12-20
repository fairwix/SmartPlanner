using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Goals.Queries;
using SmartPlanner.Domain.Entities;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SmartPlanner.Tests.Application.Goals
{
    public class GetUserGoalsQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly GetUserGoalsQueryHandler _handler;
        private readonly IMapper _mapper;

        public GetUserGoalsQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();

            // Настраиваем AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Goal, GoalDto>()
                    .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
                    .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
                    .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src => src.GetProgressPercentage()))
                    .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired()))
                    .ForMember(dest => dest.IsOnTrack, opt => opt.MapFrom(src => src.IsOnTrack()));
            });

            _mapper = config.CreateMapper();
            _handler = new GetUserGoalsQueryHandler(_mockContext.Object, _mapper);
        }

        [Fact]
        public async Task Handle_WithFilters_ShouldReturnFilteredResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var goals = new List<Goal>
            {
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Fitness Goal",
                    UserId = userId,
                    Category = GoalCategory.Sports,
                    Priority = GoalPriority.High,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsAiGenerated = false,
                    RewardAmount = 10
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Learning Goal",
                    UserId = userId,
                    Category = GoalCategory.Education,
                    Priority = GoalPriority.Medium,
                    IsCompleted = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 100,
                    IsAiGenerated = false,
                    RewardAmount = 10
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Another Fitness Goal",
                    UserId = userId,
                    Category = GoalCategory.Sports,
                    Priority = GoalPriority.Low,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsAiGenerated = false,
                    RewardAmount = 10
                }
            };

            var query = new GetUserGoalsQuery
            {
                UserId = userId,
                Category = "Sports",
                Completed = false,
                PageNumber = 1,
                PageSize = 10
            };

            var mockGoals = CreateMockDbSet(goals);
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2); // Only Sports, not completed
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(g =>
                g.Category == "Sports" && !g.IsCompleted);
        }

        [Fact]
        public async Task Handle_WithSearch_ShouldReturnMatchingResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var goals = new List<Goal>
            {
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Learn ASP.NET Core",
                    Description = "Web API development",
                    UserId = userId,
                    Category = GoalCategory.Education,
                    Priority = GoalPriority.Medium,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsAiGenerated = false,
                    RewardAmount = 10
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Fitness Challenge",
                    Description = "Daily workouts",
                    UserId = userId,
                    Category = GoalCategory.Sports,
                    Priority = GoalPriority.Medium,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsAiGenerated = false,
                    RewardAmount = 10
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = ".NET Development",
                    Description = "Learn C#",
                    UserId = userId,
                    Category = GoalCategory.Education,
                    Priority = GoalPriority.Medium,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsAiGenerated = false,
                    RewardAmount = 10
                }
            };

            var query = new GetUserGoalsQuery
            {
                UserId = userId,
                Search = ".NET",
                PageNumber = 1,
                PageSize = 10
            };

            var mockGoals = CreateMockDbSet(goals);
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2); // "Learn ASP.NET Core" and ".NET Development"
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithSorting_ShouldReturnSortedResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var goals = new List<Goal>
            {
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Z Goal",
                    UserId = userId,
                    Category = GoalCategory.Sports,
                    Priority = GoalPriority.Medium,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsAiGenerated = false,
                    RewardAmount = 10
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "A Goal",
                    UserId = userId,
                    Category = GoalCategory.Sports,
                    Priority = GoalPriority.Medium,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TargetValue = 100,
                    CurrentValue = 0,
                    IsAiGenerated = false,
                    RewardAmount = 10
                }
            };

            var query = new GetUserGoalsQuery
            {
                UserId = userId,
                SortBy = "title",
                SortOrder = "asc",
                PageNumber = 1,
                PageSize = 10
            };

            var mockGoals = CreateMockDbSet(goals);
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.Items.First().Title.Should().Be("A Goal");
            result.Items.Last().Title.Should().Be("Z Goal");
        }

        [Fact]
        public async Task Handle_NoGoals_ReturnsEmptyPagedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserGoalsQuery
            {
                UserId = userId,
                PageNumber = 1,
                PageSize = 10
            };

            var mockGoals = CreateMockDbSet(new List<Goal>());
            _mockContext.Setup(c => c.Goals).Returns(mockGoals.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            // Поддержка асинхронного перечисления
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            // Поддержка AsNoTracking
            mockSet.Setup(m => m.AsNoTracking()).Returns(mockSet.Object);

            // Поддержка Include
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);

            return mockSet;
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
