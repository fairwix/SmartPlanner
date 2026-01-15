// using AutoMapper;
// using SmartPlanner.API.Dtos.GoalsBulk;
// using SmartPlanner.Application.Goals.Commands;
// using SmartPlanner.Application.Infrastructure.Mapping;
// using Xunit;
//
// namespace SmartPlanner.Application.Tests.Infrastructure.Mapping
// {
//     public class GoalsBulkProfileTests
//     {
//         private readonly IMapper _mapper;
//
//         public GoalsBulkProfileTests()
//         {
//             var configuration = new MapperConfiguration(cfg =>
//             {
//                 cfg.AddProfile<GoalsBulkProfile>();
//             });
//
//             _mapper = configuration.CreateMapper();
//         }
//
//         [Fact]
//         public void Configure_Profile_IsValid()
//         {
//             // Arrange & Act
//             var configuration = new MapperConfiguration(cfg =>
//             {
//                 cfg.AddProfile<GoalsBulkProfile>();
//             });
//
//             // Assert
//             configuration.AssertConfigurationIsValid();
//         }
//
//         [Fact]
//         public void Map_BulkCreateGoalsRequest_To_BulkCreateGoalsCommand()
//         {
//             // Arrange
//             var request = new BulkCreateGoalsRequest
//             {
//                 Goals = new List<CreateGoalItemRequest>
//                 {
//                     new CreateGoalItemRequest
//                     {
//                         Title = "Test Goal 1",
//                         Description = "Description 1",
//                         Deadline = DateTime.UtcNow.AddDays(7)
//                     },
//                     new CreateGoalItemRequest
//                     {
//                         Title = "Test Goal 2",
//                         Description = "Description 2",
//                         Deadline = DateTime.UtcNow.AddDays(14)
//                     }
//                 },
//                 UserId = Guid.NewGuid()
//             };
//
//             // Act
//             var command = _mapper.Map<BulkCreateGoalsCommand>(request);
//
//             // Assert
//             Assert.NotNull(command);
//             Assert.Equal(request.UserId, command.UserId);
//             Assert.Equal(request.Goals.Count, command.Goals.Count);
//             Assert.Equal(request.Goals[0].Title, command.Goals[0].Title);
//             Assert.Equal(request.Goals[1].Description, command.Goals[1].Description);
//         }
//
//         [Fact]
//         public void Map_CreateGoalItemRequest_To_CreateGoalCommand()
//         {
//             // Arrange
//             var request = new CreateGoalItemRequest
//             {
//                 Title = "Test Goal",
//                 Description = "Test Description",
//                 Deadline = DateTime.UtcNow.AddDays(10),
//                 Priority = Domain.Enums.Priority.Medium,
//                 EstimatedHours = 5
//             };
//
//             // Act
//             var command = _mapper.Map<CreateGoalCommand>(request);
//
//             // Assert
//             Assert.NotNull(command);
//             Assert.Equal(request.Title, command.Title);
//             Assert.Equal(request.Description, command.Description);
//             Assert.Equal(request.Deadline, command.Deadline);
//             Assert.Equal(request.Priority, command.Priority);
//             Assert.Equal(request.EstimatedHours, command.EstimatedHours);
//         }
//
//         [Fact]
//         public void Map_BulkUpdateGoalsRequest_To_BulkUpdateGoalsCommand()
//         {
//             // Arrange
//             var request = new BulkUpdateGoalsRequest
//             {
//                 Goals = new List<UpdateGoalItemRequest>
//                 {
//                     new UpdateGoalItemRequest
//                     {
//                         Id = Guid.NewGuid(),
//                         Title = "Updated Goal 1",
//                         IsCompleted = true
//                     },
//                     new UpdateGoalItemRequest
//                     {
//                         Id = Guid.NewGuid(),
//                         Title = "Updated Goal 2",
//                         Description = "Updated Description"
//                     }
//                 },
//                 UserId = Guid.NewGuid()
//             };
//
//             // Act
//             var command = _mapper.Map<BulkUpdateGoalsCommand>(request);
//
//             // Assert
//             Assert.NotNull(command);
//             Assert.Equal(request.UserId, command.UserId);
//             Assert.Equal(request.Goals.Count, command.Goals.Count);
//             Assert.Equal(request.Goals[0].Id, command.Goals[0].Id);
//             Assert.Equal(request.Goals[1].Title, command.Goals[1].Title);
//         }
//
//         [Fact]
//         public void Map_UpdateGoalItemRequest_To_UpdateGoalCommand()
//         {
//             // Arrange
//             var goalId = Guid.NewGuid();
//             var request = new UpdateGoalItemRequest
//             {
//                 Id = goalId,
//                 Title = "Updated Title",
//                 Description = "Updated Description",
//                 IsCompleted = true,
//                 CompletedAt = DateTime.UtcNow
//             };
//
//             // Act
//             var command = _mapper.Map<UpdateGoalCommand>(request);
//
//             // Assert
//             Assert.NotNull(command);
//             Assert.Equal(request.Id, command.Id);
//             Assert.Equal(request.Title, command.Title);
//             Assert.Equal(request.Description, command.Description);
//             Assert.Equal(request.IsCompleted, command.IsCompleted);
//             Assert.Equal(request.CompletedAt, command.CompletedAt);
//         }
//
//         [Fact]
//         public void Map_BulkDeleteGoalsRequest_To_BulkDeleteGoalsCommand()
//         {
//             // Arrange
//             var request = new BulkDeleteGoalsRequest
//             {
//                 GoalIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
//                 UserId = Guid.NewGuid()
//             };
//
//             // Act
//             var command = _mapper.Map<BulkDeleteGoalsCommand>(request);
//
//             // Assert
//             Assert.NotNull(command);
//             Assert.Equal(request.UserId, command.UserId);
//             Assert.Equal(request.GoalIds, command.GoalIds);
//             Assert.Equal(3, command.GoalIds.Count);
//         }
//
//         [Fact]
//         public void Map_CreateGoalItemRequest_WithNullValues_To_CreateGoalCommand()
//         {
//             // Arrange
//             var request = new CreateGoalItemRequest
//             {
//                 Title = "Test Goal",
//                 Description = null,
//                 Deadline = null,
//                 Priority = null
//             };
//
//             // Act
//             var command = _mapper.Map<CreateGoalCommand>(request);
//
//             // Assert
//             Assert.NotNull(command);
//             Assert.Equal(request.Title, command.Title);
//             Assert.Null(command.Description);
//             Assert.Null(command.Deadline);
//             Assert.Null(command.Priority);
//         }
//
//         [Fact]
//         public void Map_BulkCreateGoalsRequest_WithEmptyList_To_BulkCreateGoalsCommand()
//         {
//             // Arrange
//             var request = new BulkCreateGoalsRequest
//             {
//                 Goals = new List<CreateGoalItemRequest>(),
//                 UserId = Guid.NewGuid()
//             };
//
//             // Act
//             var command = _mapper.Map<BulkCreateGoalsCommand>(request);
//
//             // Assert
//             Assert.NotNull(command);
//             Assert.Equal(request.UserId, command.UserId);
//             Assert.Empty(command.Goals);
//         }
//     }
// }
