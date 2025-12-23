// // Tests/Unit/Application/Challenges/Queries/GetUserChallengesQueryHandlerTests.cs
// using AutoMapper;
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Moq;
// using SmartPlanner.Application.Challenges.Dtos;
// using SmartPlanner.Application.Challenges.Queries;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Domain.Entities;
// using Xunit;
//
// namespace SmartPlanner.Application.UnitTests.Challenges.Queries
// {
//     public class GetUserChallengesQueryHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _contextMock;
//         private readonly IMapper _mapper;
//         private readonly GetUserChallengesQueryHandler _handler;
//
//         public GetUserChallengesQueryHandlerTests()
//         {
//             _contextMock = new Mock<IApplicationDbContext>();
//
//             var config = new MapperConfiguration(cfg =>
//             {
//                 cfg.CreateMap<Challenge, ChallengeDto>()
//                     .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
//                     .ForMember(dest => dest.GroupProgressPercentage, opt => opt.MapFrom(src =>
//                         src.TargetValue > 0 ? (double)src.CurrentValue / src.TargetValue * 100 : 0));
//
//                 cfg.CreateMap<ChallengeParticipant, ChallengeParticipantDto>()
//                     .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
//             });
//
//             _mapper = config.CreateMapper();
//             _handler = new GetUserChallengesQueryHandler(_contextMock.Object, _mapper);
//         }
//
//         private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
//         {
//             var queryable = data.AsQueryable();
//             var mockSet = new Mock<DbSet<T>>();
//
//             mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
//             mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
//             mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
//             mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
//
//             mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
//
//             return mockSet;
//         }
//
//         private List<Challenge> CreateTestData()
//         {
//             var userId = Guid.NewGuid();
//             var otherUserId = Guid.NewGuid();
//             var now = DateTime.UtcNow;
//
//             return new List<Challenge>
//             {
//                 new Challenge
//                 {
//                     Id = Guid.NewGuid(),
//                     Title = "My Learning Challenge",
//                     Type = ChallengeType.Learning,
//                     CreatedBy = userId,
//                     StartDate = now.AddDays(-10),
//                     EndDate = now.AddDays(20),
//                     TargetValue = 20,
//                     CurrentValue = 10,
//                     CreatedAt = now.AddDays(-10),
//                     UpdatedAt = now,
//                     Participants = new List<ChallengeParticipant>()
//                 },
//                 new Challenge
//                 {
//                     Id = Guid.NewGuid(),
//                     Title = "Participant Exercise Challenge",
//                     Type = ChallengeType.Exercise,
//                     CreatedBy = otherUserId,
//                     StartDate = now.AddDays(-5),
//                     EndDate = now.AddDays(15),
//                     TargetValue = 30,
//                     CurrentValue = 15,
//                     CreatedAt = now.AddDays(-5),
//                     UpdatedAt = now,
//                     Participants = new List<ChallengeParticipant>
//                     {
//                         new ChallengeParticipant
//                         {
//                             Id = Guid.NewGuid(),
//                             UserId = userId,
//                             Username = "myusername",
//                             Status = ParticipantStatus.Joined,
//                             PersonalContribution = 10,
//                             JoinedAt = now.AddDays(-4),
//                             CreatedAt = now.AddDays(-4),
//                             UpdatedAt = now.AddDays(-4)
//                         }
//                     }
//                 },
//                 new Challenge
//                 {
//                     Id = Guid.NewGuid(),
//                     Title = "Completed Custom Challenge",
//                     Type = ChallengeType.Custom,
//                     CreatedBy = userId,
//                     StartDate = now.AddDays(-30),
//                     EndDate = now.AddDays(-1),
//                     TargetValue = 50,
//                     CurrentValue = 50,
//                     CreatedAt = now.AddDays(-30),
//                     UpdatedAt = now.AddDays(-1),
//                     Participants = new List<ChallengeParticipant>()
//                 },
//                 new Challenge
//                 {
//                     Id = Guid.NewGuid(),
//                     Title = "Other User Challenge",
//                     Type = ChallengeType.StepCount,
//                     CreatedBy = otherUserId,
//                     StartDate = now.AddDays(-5),
//                     EndDate = now.AddDays(25),
//                     TargetValue = 100,
//                     CurrentValue = 0,
//                     CreatedAt = now.AddDays(-5),
//                     UpdatedAt = now,
//                     Participants = new List<ChallengeParticipant>()
//                 }
//             };
//         }
//
//         [Fact]
//         public async Task Handle_ReturnsUserChallengesAndParticipations()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var userId = testData.First().CreatedBy;
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetUserChallengesQuery { UserId = userId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(3); // Два созданных + один участник
//             result.Select(c => c.Title).Should().Contain("My Learning Challenge");
//             result.Select(c => c.Title).Should().Contain("Participant Exercise Challenge");
//             result.Select(c => c.Title).Should().Contain("Completed Custom Challenge");
//         }
//
//         [Fact]
//         public async Task Handle_FiltersOutCompletedChallenges_WhenIncludeCompletedIsFalse()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var userId = testData.First().CreatedBy;
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetUserChallengesQuery
//             {
//                 UserId = userId,
//                 IncludeCompleted = false
//             };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(2); // Только незавершенные
//             result.Select(c => c.Title).Should().Contain("My Learning Challenge");
//             result.Select(c => c.Title).Should().Contain("Participant Exercise Challenge");
//             result.Select(c => c.Title).Should().NotContain("Completed Custom Challenge");
//         }
//
//         [Fact]
//         public async Task Handle_IncludesCompletedChallenges_WhenIncludeCompletedIsTrue()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var userId = testData.First().CreatedBy;
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetUserChallengesQuery
//             {
//                 UserId = userId,
//                 IncludeCompleted = true
//             };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(3); // Все включая завершенные
//             result.Select(c => c.Title).Should().Contain("Completed Custom Challenge");
//         }
//
//         [Fact]
//         public async Task Handle_UserHasNoChallenges_ReturnsEmptyList()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var nonExistentUserId = Guid.NewGuid();
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetUserChallengesQuery { UserId = nonExistentUserId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().BeEmpty();
//         }
//
//         [Fact]
//         public async Task Handle_IncludesParticipantDetails()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var userId = testData.First().CreatedBy;
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetUserChallengesQuery { UserId = userId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//
//             var participantChallenge = result.First(c => c.Title == "Participant Exercise Challenge");
//             participantChallenge.Participants.Should().HaveCount(1);
//             participantChallenge.Participants.First().Username.Should().Be("myusername");
//             participantChallenge.Participants.First().Status.Should().Be("Joined");
//             participantChallenge.Participants.First().PersonalContribution.Should().Be(10);
//         }
//     }
// }
