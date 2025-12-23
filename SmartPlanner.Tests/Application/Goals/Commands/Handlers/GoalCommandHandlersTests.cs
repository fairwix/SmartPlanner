// Tests/Unit/Application/Goals/Commands/Handlers/GoalCommandHandlersTests.cs
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;
using GoalCategory = SmartPlanner.Domain.Entities.GoalCategory;

namespace SmartPlanner.Application.UnitTests.Goals.Commands.Handlers
{
    public class CreateGoalCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<ILogger<CreateGoalCommandHandler>> _loggerMock;
        private readonly IMapper _mapper;
        private readonly CreateGoalCommandHandler _handler;

        public CreateGoalCommandHandlerTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _loggerMock = new Mock<ILogger<CreateGoalCommandHandler>>();

            // Настройка AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Goal, GoalDto>()
                    .ConstructUsing(g => new GoalDto(
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
            });
            _mapper = config.CreateMapper();

            _handler = new CreateGoalCommandHandler(
                _contextMock.Object,
                _loggerMock.Object,
                _mapper);
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

        [Fact]
        public async Task Handle_ShouldCreateGoalSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var users = new List<User> { new User { Id = userId } };
            var goals = new List<Goal>();

            var usersMockSet = CreateMockDbSet(users);
            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Users).Returns(usersMockSet.Object);
            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateGoalCommand
            {
                Title = "Learn Spanish",
                Description = "Complete Spanish course",
                Category = GoalCategory.Education.ToString(),
                Priority = GoalPriority.High.ToString(),
                DueDate = DateTime.UtcNow.AddMonths(3),
                TargetValue = 100,
                UserId = userId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Learn Spanish");
            result.Category.Should().Be("Education");
            result.Priority.Should().Be("High");
            result.TargetValue.Should().Be(100);
            result.UserId.Should().Be(userId);

            _contextMock.Verify(c => c.Goals.AddAsync(It.IsAny<Goal>(), It.IsAny<CancellationToken>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var users = new List<User>();
            var usersMockSet = CreateMockDbSet(users);

            _contextMock.Setup(c => c.Users).Returns(usersMockSet.Object);

            var command = new CreateGoalCommand
            {
                Title = "Test Goal",
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage($"*User with ID {command.UserId} not found*");
        }

        [Fact]
        public async Task Handle_ShouldSetDefaultRewardAmount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var users = new List<User> { new User { Id = userId } };
            var goals = new List<Goal>();

            var usersMockSet = CreateMockDbSet(users);
            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Users).Returns(usersMockSet.Object);
            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateGoalCommand
            {
                Title = "Test Goal",
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = userId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.RewardAmount.Should().Be(10); // Default from handler
        }
    }

    public class DeleteGoalCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly DeleteGoalCommandHandler _handler;

        public DeleteGoalCommandHandlerTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _handler = new DeleteGoalCommandHandler(_contextMock.Object);
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

        [Fact]
        public async Task Handle_ShouldDeleteGoalSuccessfully()
        {
            // Arrange
            var goalId = Guid.NewGuid();
            var goal = new Goal { Id = goalId, Title = "Test Goal" };
            var goals = new List<Goal> { goal };

            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new DeleteGoalCommand { GoalId = goalId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _contextMock.Verify(c => c.Goals.Remove(goal), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenGoalNotFound()
        {
            // Arrange
            var goals = new List<Goal>();
            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);

            var command = new DeleteGoalCommand { GoalId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            _contextMock.Verify(c => c.Goals.Remove(It.IsAny<Goal>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class UpdateGoalProgressCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<ILogger<UpdateGoalProgressCommandHandler>> _loggerMock;
        private readonly UpdateGoalProgressCommandHandler _handler;

        public UpdateGoalProgressCommandHandlerTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _loggerMock = new Mock<ILogger<UpdateGoalProgressCommandHandler>>();
            _handler = new UpdateGoalProgressCommandHandler(_contextMock.Object, _loggerMock.Object);
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

        [Fact]
        public async Task Handle_ShouldUpdateProgressSuccessfully()
        {
            // Arrange
            var goalId = Guid.NewGuid();
            var goal = new Goal
            {
                Id = goalId,
                Title = "Test Goal",
                TargetValue = 100,
                CurrentValue = 50,
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Category = GoalCategory.Health,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(30),
                Description = "Test Description",
                IsAiGenerated = false,
                RewardAmount = 10
            };

            var goals = new List<Goal> { goal };
            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateGoalProgressCommand
            {
                GoalId = goalId,
                Value = 25
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentValue.Should().Be(75); // 50 + 25
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenGoalNotFound()
        {
            // Arrange
            var goals = new List<Goal>();
            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);

            var command = new UpdateGoalProgressCommand
            {
                GoalId = Guid.NewGuid(),
                Value = 25
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldCompleteGoal_WhenProgressReachesTarget()
        {
            // Arrange
            var goalId = Guid.NewGuid();
            var goal = new Goal
            {
                Id = goalId,
                Title = "Test Goal",
                TargetValue = 100,
                CurrentValue = 90,
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Category = GoalCategory.Health,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(30),
                Description = "Test Description",
                IsAiGenerated = false,
                RewardAmount = 10
            };

            var goals = new List<Goal> { goal };
            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateGoalProgressCommand
            {
                GoalId = goalId,
                Value = 10
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.CurrentValue.Should().Be(100);
            result!.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldLogProgressUpdate()
        {
            // Arrange
            var goalId = Guid.NewGuid();
            var goal = new Goal
            {
                Id = goalId,
                Title = "Test Goal",
                TargetValue = 100,
                CurrentValue = 50,
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Category = GoalCategory.Health,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(30),
                Description = "Test Description",
                IsAiGenerated = false,
                RewardAmount = 10
            };

            var goals = new List<Goal> { goal };
            var goalsMockSet = CreateMockDbSet(goals);

            _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateGoalProgressCommand
            {
                GoalId = goalId,
                Value = 25
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Progress updated")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
