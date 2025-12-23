// // Tests/Unit/Application/Challenges/Queries/GetChallengesQueryHandlerTests.cs
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
//     public class GetChallengesQueryHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _contextMock;
//         private readonly IMapper _mapper;
//         private readonly GetChallengesQueryHandler _handler;
//
//         public GetChallengesQueryHandlerTests()
//         {
//             _contextMock = new Mock<IApplicationDbContext>();
//
//             // Настройка AutoMapper - упрощенная версия
//             var config = new MapperConfiguration(cfg =>
//             {
//                 // Маппинг для Challenge
//                 cfg.CreateMap<Challenge, ChallengeDto>()
//                     .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
//                     .ForMember(dest => dest.GroupProgressPercentage, opt => opt.MapFrom(src =>
//                         src.TargetValue > 0 ? (double)src.CurrentValue / src.TargetValue * 100 : 0));
//
//                 // Маппинг для ChallengeParticipant
//                 cfg.CreateMap<ChallengeParticipant, ChallengeParticipantDto>()
//                     .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
//             });
//
//             _mapper = config.CreateMapper();
//             _handler = new GetChallengesQueryHandler(_contextMock.Object, _mapper);
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
//             // Настройка для Include
//             mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
//
//             return mockSet;
//         }
//
//         private List<Challenge> CreateTestData()
//         {
//             var userId = Guid.NewGuid();
//             var now = DateTime.UtcNow;
//
//             return new List<Challenge>
//             {
//                 new Challenge
//                 {
//                     Id = Guid.NewGuid(),
//                     Title = "Active Step Challenge",
//                     Description = "Description 1",
//                     Type = ChallengeType.StepCount,
//                     StartDate = now.AddDays(-5),
//                     EndDate = now.AddDays(5),
//                     IsGroupChallenge = true,
//                     TargetValue = 10000,
//                     CurrentValue = 5000,
//                     IsActive = true,
//                     CreatedBy = userId,
//                     CreatedAt = now.AddDays(-5),
//                     UpdatedAt = now,
//                     Participants = new List<ChallengeParticipant>
//                     {
//                         new ChallengeParticipant
//                         {
//                             Id = Guid.NewGuid(),
//                             UserId = userId,
//                             Username = "testuser", // Исправлено: должно быть свойство
//                             Status = ParticipantStatus.Joined,
//                             PersonalContribution = 2500,
//                             JoinedAt = now.AddDays(-4),
//                             CreatedAt = now.AddDays(-4),
//                             UpdatedAt = now.AddDays(-4)
//                         }
//                     }
//                 },
//                 new Challenge
//                 {
//                     Id = Guid.NewGuid(),
//                     Title = "Completed Reading Challenge",
//                     Description = "Description 2",
//                     Type = ChallengeType.Reading,
//                     StartDate = now.AddDays(-30),
//                     EndDate = now.AddDays(-1),
//                     IsGroupChallenge = false,
//                     TargetValue = 10,
//                     CurrentValue = 10,
//                     IsActive = false,
//                     CreatedBy = Guid.NewGuid(),
//                     CreatedAt = now.AddDays(-30),
//                     UpdatedAt = now.AddDays(-1),
//                     Participants = new List<ChallengeParticipant>()
//                 }
//             };
//         }
//
//         [Fact]
//         public async Task Handle_NoFilters_ReturnsAllChallenges()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery();
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(2);
//             _contextMock.Verify(c => c.Challenges, Times.AtLeastOnce);
//         }
//
//         [Fact]
//         public async Task Handle_ActiveOnlyTrue_ReturnsActiveChallenges()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery { ActiveOnly = true };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(1);
//             result.First().Title.Should().Be("Active Step Challenge");
//             result.First().Type.Should().Be(ChallengeType.StepCount.ToString());
//         }
//
//         [Theory]
//         [InlineData("StepCount", 1)]
//         [InlineData("Reading", 1)]
//         [InlineData("Exercise", 0)] // Нет таких в тестовых данных
//         public async Task Handle_WithTypeFilter_ReturnsFilteredChallenges(string type, int expectedCount)
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery { Type = type };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(expectedCount);
//         }
//
//         [Fact]
//         public async Task Handle_WithUserId_ReturnsUserChallenges()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var userId = testData.First().CreatedBy;
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery { UserId = userId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(1); // Только первый создан пользователем
//             result.First().CreatedBy.Should().Be(userId);
//         }
//
//         [Fact]
//         public async Task Handle_WithIsGroupChallengeTrue_ReturnsGroupChallenges()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery { IsGroupChallenge = true };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().HaveCount(1);
//             result.First().IsGroupChallenge.Should().BeTrue();
//         }
//
//         [Fact]
//         public async Task Handle_IncludesParticipants()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery();
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             var challengeWithParticipants = result.First(c => c.Participants.Any());
//             challengeWithParticipants.Participants.Should().HaveCount(1);
//             challengeWithParticipants.Participants.First().Username.Should().Be("testuser");
//             challengeWithParticipants.Participants.First().Status.Should().Be("Joined");
//         }
//
//         [Fact]
//         public async Task Handle_CalculatesGroupProgressPercentage()
//         {
//             // Arrange
//             var testData = CreateTestData();
//             var mockSet = CreateMockDbSet(testData);
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery();
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//
//             var challenge1 = result.First(c => c.Title == "Active Step Challenge");
//             challenge1.GroupProgressPercentage.Should().Be(50.0); // 5000/10000*100
//
//             var challenge2 = result.First(c => c.Title == "Completed Reading Challenge");
//             challenge2.GroupProgressPercentage.Should().Be(100.0); // 10/10*100
//         }
//
//         [Fact]
//         public async Task Handle_EmptyDatabase_ReturnsEmptyList()
//         {
//             // Arrange
//             var mockSet = CreateMockDbSet(new List<Challenge>());
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengesQuery();
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result.Should().BeEmpty();
//         }
//
//         // В классе GetChallengesQueryHandlerTests исправьте только проблемный метод:
//
// [Fact]
// public async Task Handle_ParticipantStatusMapping_WorksCorrectly()
// {
//     // Arrange
//     var challenge = new Challenge
//     {
//         Id = Guid.NewGuid(),
//         Title = "Test Challenge",
//         Type = ChallengeType.Custom,
//         CreatedBy = Guid.NewGuid(),
//         StartDate = DateTime.UtcNow,
//         EndDate = DateTime.UtcNow.AddDays(30),
//         TargetValue = 100,
//         CurrentValue = 0,
//         IsActive = true,
//         Participants = new List<ChallengeParticipant>
//         {
//             new ChallengeParticipant
//             {
//                 Id = Guid.NewGuid(),
//                 UserId = Guid.NewGuid(),
//                 Username = "user1",
//                 Status = ParticipantStatus.Invited // Исправлено
//             },
//             new ChallengeParticipant
//             {
//                 Id = Guid.NewGuid(),
//                 UserId = Guid.NewGuid(),
//                 Username = "user2",
//                 Status = ParticipantStatus.Joined
//             },
//             new ChallengeParticipant
//             {
//                 Id = Guid.NewGuid(),
//                 UserId = Guid.NewGuid(),
//                 Username = "user3",
//                 Status = ParticipantStatus.Completed
//             }
//             // Убрали ParticipantStatus.Left, так как его нет в вашем enum
//         }
//     };
//
//     var mockSet = CreateMockDbSet(new List<Challenge> { challenge });
//
//     _contextMock.Setup(c => c.Challenges)
//         .Returns(mockSet.Object);
//
//     var query = new GetChallengesQuery();
//
//     // Act
//     var result = await _handler.Handle(query, CancellationToken.None);
//
//     // Assert
//     result.Should().NotBeNull();
//     var challengeDto = result.First();
//     challengeDto.Participants.Should().HaveCount(3);
//
//     // Проверяем маппинг статусов
//     challengeDto.Participants.Select(p => p.Status).Should().ContainInOrder(
//         "Invited",
//         "Joined",
//         "Completed"
//     );
//
//     // Проверяем, что имена пользователей сохранены
//     challengeDto.Participants.Select(p => p.Username).Should().ContainInOrder(
//         "user1",
//         "user2",
//         "user3"
//     );
//
//         }
//     }
// }
