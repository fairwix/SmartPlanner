// // Tests/Unit/Application/Goals/Commands/Handlers/GoalCommandEdgeCasesTests.cs
// using AutoMapper;
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Moq;
// using SmartPlanner.Application.Common.Dtos;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Application.Goals.Commands;
// using SmartPlanner.Application.Goals.Dtos;
// using SmartPlanner.Domain.Entities;
// using Xunit;
// using GoalCategory = SmartPlanner.Domain.Entities.GoalCategory;
//
// namespace SmartPlanner.Application.UnitTests.Goals.Commands.Handlers
// {
//     public class GoalCommandEdgeCasesTests
//     {
//         public class CreateGoalCommandHandlerEdgeCasesTests
//         {
//             private readonly Mock<IApplicationDbContext> _contextMock;
//             private readonly Mock<ILogger<CreateGoalCommandHandler>> _loggerMock;
//             private readonly IMapper _mapper;
//             private readonly CreateGoalCommandHandler _handler;
//
//             public CreateGoalCommandHandlerEdgeCasesTests()
//             {
//                 _contextMock = new Mock<IApplicationDbContext>();
//                 _loggerMock = new Mock<ILogger<CreateGoalCommandHandler>>();
//
//                 var config = new MapperConfiguration(cfg =>
//                 {
//                     cfg.CreateMap<Goal, GoalDto>();
//                 });
//                 _mapper = config.CreateMapper();
//
//                 _handler = new CreateGoalCommandHandler(
//                     _contextMock.Object,
//                     _loggerMock.Object,
//                     _mapper);
//             }
//
//             private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
//             {
//                 var queryable = data.AsQueryable();
//                 var mockSet = new Mock<DbSet<T>>();
//
//                 mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
//                 mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
//                 mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
//                 mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
//
//                 return mockSet;
//             }
//
//             [Fact]
//             public async Task Handle_ShouldHandleMaximumTargetValue()
//             {
//                 // Arrange
//                 var userId = Guid.NewGuid();
//                 var users = new List<User> { new User { Id = userId } };
//                 var goals = new List<Goal>();
//
//                 var usersMockSet = CreateMockDbSet(users);
//                 var goalsMockSet = CreateMockDbSet(goals);
//
//                 _contextMock.Setup(c => c.Users).Returns(usersMockSet.Object);
//                 _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
//                 _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                     .ReturnsAsync(1);
//
//                 var command = new CreateGoalCommand
//                 {
//                     Title = "Large Goal",
//                     Category = GoalCategory.Health.ToString(),
//                     Priority = GoalPriority.Medium.ToString(),
//                     DueDate = DateTime.UtcNow.AddDays(30),
//                     TargetValue = int.MaxValue, // Максимальное значение
//                     UserId = userId
//                 };
//
//                 // Act
//                 var result = await _handler.Handle(command, CancellationToken.None);
//
//                 // Assert
//                 result.Should().NotBeNull();
//                 result.TargetValue.Should().Be(int.MaxValue);
//             }
//
//             [Fact]
//             public async Task Handle_ShouldHandleMinimumTargetValue()
//             {
//                 // Arrange
//                 var userId = Guid.NewGuid();
//                 var users = new List<User> { new User { Id = userId } };
//                 var goals = new List<Goal>();
//
//                 var usersMockSet = CreateMockDbSet(users);
//                 var goalsMockSet = CreateMockDbSet(goals);
//
//                 _contextMock.Setup(c => c.Users).Returns(usersMockSet.Object);
//                 _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
//                 _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                     .ReturnsAsync(1);
//
//                 var command = new CreateGoalCommand
//                 {
//                     Title = "Small Goal",
//                     Category = GoalCategory.Health.ToString(),
//                     Priority = GoalPriority.Medium.ToString(),
//                     DueDate = DateTime.UtcNow.AddDays(30),
//                     TargetValue = 1, // Минимальное значение
//                     UserId = userId
//                 };
//
//                 // Act
//                 var result = await _handler.Handle(command, CancellationToken.None);
//
//                 // Assert
//                 result.Should().NotBeNull();
//                 result.TargetValue.Should().Be(1);
//             }
//         }
//
//         public class BulkCreateGoalsCommandHandlerEdgeCasesTests
//         {
//             private readonly Mock<IApplicationDbContext> _contextMock;
//             private readonly Mock<ILogger<BulkCreateGoalsCommandHandler>> _loggerMock;
//             private readonly IMapper _mapper;
//             private readonly BulkCreateGoalsCommandHandler _handler;
//
//             public BulkCreateGoalsCommandHandlerEdgeCasesTests()
//             {
//                 _contextMock = new Mock<IApplicationDbContext>();
//                 _loggerMock = new Mock<ILogger<BulkCreateGoalsCommandHandler>>();
//
//                 var config = new MapperConfiguration(cfg =>
//                 {
//                     cfg.CreateMap<Goal, GoalDto>();
//                 });
//                 _mapper = config.CreateMapper();
//
//                 _handler = new BulkCreateGoalsCommandHandler(
//                     _contextMock.Object,
//                     _mapper,
//                     _loggerMock.Object);
//             }
//
//             private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
//             {
//                 var queryable = data.AsQueryable();
//                 var mockSet = new Mock<DbSet<T>>();
//
//                 mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
//                 mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
//                 mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
//                 mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
//
//                 return mockSet;
//             }
//
//             [Fact]
//             public async Task Handle_ShouldHandleEmptyGoalsList()
//             {
//                 // Arrange
//                 var userId = Guid.NewGuid();
//                 var users = new List<User> { new User { Id = userId } };
//
//                 var usersMockSet = CreateMockDbSet(users);
//
//                 _contextMock.Setup(c => c.Users).Returns(usersMockSet.Object);
//
//                 var command = new BulkCreateGoalsCommand
//                 {
//                     UserId = userId,
//                     Goals = new List<CreateGoalDto>() // Пустой список
//                 };
//
//                 // Act
//                 var result = await _handler.Handle(command, CancellationToken.None);
//
//                 // Assert
//                 result.Should().NotBeNull();
//                 result.TotalCount.Should().Be(0);
//                 result.SuccessfulCount.Should().Be(0);
//                 result.FailedCount.Should().Be(0);
//                 result.Items.Should().BeEmpty();
//             }
//
//             [Fact]
//             public async Task Handle_ShouldHandleMixedSuccessAndFailure()
//             {
//                 // Arrange
//                 var userId = Guid.NewGuid();
//                 var users = new List<User> { new User { Id = userId } };
//                 // Уже существует цель с названием "Goal 2"
//                 var existingGoals = new List<Goal>
//                 {
//                     new Goal { Id = Guid.NewGuid(), Title = "Goal 2", UserId = userId }
//                 };
//
//                 var usersMockSet = CreateMockDbSet(users);
//                 var goalsMockSet = CreateMockDbSet(existingGoals);
//
//                 _contextMock.Setup(c => c.Users).Returns(usersMockSet.Object);
//                 _contextMock.Setup(c => c.Goals).Returns(goalsMockSet.Object);
//                 _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
//                     .ReturnsAsync(2); // 2 успешных сохранения
//
//                 var command = new BulkCreateGoalsCommand
//                 {
//                     UserId = userId,
//                     Goals = new List<CreateGoalDto>
//                     {
//                         new CreateGoalDto
//                         {
//                             Title = "Goal 1",
//                             Category = "Health",
//                             DueDate = DateTime.UtcNow.AddDays(30),
//                             TargetValue = 100
//                         },
//                         new CreateGoalDto
//                         {
//                             Title = "Goal 2", // Дубликат - неудача
//                             Category = "Education",
//                             DueDate = DateTime.UtcNow.AddDays(60),
//                             TargetValue = 50
//                         },
//                         new CreateGoalDto
//                         {
//                             Title = "Goal 3",
//                             Category = "Finance",
//                             DueDate = DateTime.UtcNow.AddDays(90),
//                             TargetValue = 200
//                         }
//                     }
//                 };
//
//                 // Act
//                 var result = await _handler.Handle(command, CancellationToken.None);
//
//                 // Assert
//                 result.Should().NotBeNull();
//                 result.TotalCount.Should().Be(3);
//                 result.SuccessfulCount.Should().Be(2); // Goal 1 и Goal 3
//                 result.FailedCount.Should().Be(1);    // Goal 2
//                 result.SuccessRate.Should().BeApproximately(66.67, 0.01);
//             }
//         }
//     }
// }
