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
//     public class GetChallengeByIdQueryHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _contextMock;
//         private readonly IMapper _mapper;
//         private readonly GetChallengeByIdQueryHandler _handler;
//
//         public GetChallengeByIdQueryHandlerTests()
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
//             _handler = new GetChallengeByIdQueryHandler(_contextMock.Object, _mapper);
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
//         [Fact]
//         public async Task Handle_ChallengeExists_ReturnsChallengeDto()
//         {
//             // Arrange
//             var challengeId = Guid.NewGuid();
//             var challenge = new Challenge
//             {
//                 Id = challengeId,
//                 Title = "Test Challenge",
//                 Description = "Test Description",
//                 Type = ChallengeType.StepCount,
//                 CreatedBy = Guid.NewGuid(),
//                 StartDate = DateTime.UtcNow,
//                 EndDate = DateTime.UtcNow.AddDays(30),
//                 TargetValue = 10000,
//                 CurrentValue = 5000,
//                 IsActive = true,
//                 CreatedAt = DateTime.UtcNow,
//                 UpdatedAt = DateTime.UtcNow,
//                 Participants = new List<ChallengeParticipant>
//                 {
//                     new ChallengeParticipant
//                     {
//                         Id = Guid.NewGuid(),
//                         UserId = Guid.NewGuid(),
//                         Username = "participant1",
//                         Status = ParticipantStatus.Invited,
//                         PersonalContribution = 1000,
//                         JoinedAt = DateTime.UtcNow,
//                         CreatedAt = DateTime.UtcNow,
//                         UpdatedAt = DateTime.UtcNow
//                     }
//                 }
//             };
//
//             var mockSet = CreateMockDbSet(new List<Challenge> { challenge });
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengeByIdQuery { ChallengeId = challengeId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result!.Id.Should().Be(challengeId);
//             result.Title.Should().Be("Test Challenge");
//             result.Type.Should().Be(ChallengeType.StepCount.ToString());
//             result.GroupProgressPercentage.Should().Be(50.0);
//             result.Participants.Should().HaveCount(1);
//             result.Participants.First().Status.Should().Be("Invited");
//             result.Participants.First().Username.Should().Be("participant1");
//         }
//
//         [Fact]
//         public async Task Handle_ChallengeDoesNotExist_ReturnsNull()
//         {
//             // Arrange
//             var nonExistentId = Guid.NewGuid();
//             var mockSet = CreateMockDbSet(new List<Challenge>());
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengeByIdQuery { ChallengeId = nonExistentId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().BeNull();
//         }
//
//         [Fact]
//         public async Task Handle_IncludesAllParticipantsWithCorrectStatuses()
//         {
//             // Arrange
//             var challengeId = Guid.NewGuid();
//             var challenge = new Challenge
//             {
//                 Id = challengeId,
//                 Title = "All Statuses Challenge",
//                 Type = ChallengeType.Reading,
//                 CreatedBy = Guid.NewGuid(),
//                 TargetValue = 10,
//                 CurrentValue = 5,
//                 IsActive = true,
//                 Participants = new List<ChallengeParticipant>
//                 {
//                     new ChallengeParticipant
//                     {
//                         Id = Guid.NewGuid(),
//                         UserId = Guid.NewGuid(),
//                         Username = "user1",
//                         Status = ParticipantStatus.Invited, // Исправлено: Invited вместо Left
//                         PersonalContribution = 0
//                     },
//                     new ChallengeParticipant
//                     {
//                         Id = Guid.NewGuid(),
//                         UserId = Guid.NewGuid(),
//                         Username = "user2",
//                         Status = ParticipantStatus.Joined,
//                         PersonalContribution = 3
//                     },
//                     new ChallengeParticipant
//                     {
//                         Id = Guid.NewGuid(),
//                         UserId = Guid.NewGuid(),
//                         Username = "user3",
//                         Status = ParticipantStatus.Completed,
//                         PersonalContribution = 5
//                     }
//                     // Убрали ParticipantStatus.Left, так как его нет в вашем enum
//                 }
//             };
//
//             var mockSet = CreateMockDbSet(new List<Challenge> { challenge });
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengeByIdQuery { ChallengeId = challengeId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result!.Participants.Should().HaveCount(3);
//             result.Participants.Select(p => p.Status).Should().ContainInOrder(
//                 "Invited",
//                 "Joined",
//                 "Completed"
//             );
//
//             // Проверяем маппинг Username
//             result.Participants.Select(p => p.Username).Should().ContainInOrder(
//                 "user1",
//                 "user2",
//                 "user3"
//             );
//
//             // Проверяем маппинг PersonalContribution
//             result.Participants.Select(p => p.PersonalContribution).Should().ContainInOrder(0, 3, 5);
//         }
//
//         [Fact]
//         public async Task Handle_ChallengeWithNoParticipants_ReturnsEmptyParticipantsList()
//         {
//             // Arrange
//             var challengeId = Guid.NewGuid();
//             var challenge = new Challenge
//             {
//                 Id = challengeId,
//                 Title = "Solo Challenge",
//                 Type = ChallengeType.Exercise,
//                 CreatedBy = Guid.NewGuid(),
//                 TargetValue = 50,
//                 CurrentValue = 25,
//                 IsActive = true,
//                 Participants = new List<ChallengeParticipant>()
//             };
//
//             var mockSet = CreateMockDbSet(new List<Challenge> { challenge });
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengeByIdQuery { ChallengeId = challengeId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result!.Participants.Should().NotBeNull();
//             result.Participants.Should().BeEmpty();
//             result.GroupProgressPercentage.Should().Be(50.0); // 25/50*100
//         }
//
//         [Fact]
//         public async Task Handle_ChallengeWithAllValidParticipantStatuses()
//         {
//             // Arrange
//             var challengeId = Guid.NewGuid();
//             var challenge = new Challenge
//             {
//                 Id = challengeId,
//                 Title = "Valid Statuses Challenge",
//                 Type = ChallengeType.Custom,
//                 CreatedBy = Guid.NewGuid(),
//                 TargetValue = 100,
//                 CurrentValue = 75,
//                 IsActive = true,
//                 Participants = new List<ChallengeParticipant>()
//             };
//
//             // Добавляем участников со всеми валидными статусами из вашего enum
//             var validStatuses = Enum.GetValues(typeof(ParticipantStatus)).Cast<ParticipantStatus>().ToList();
//
//             for (int i = 0; i < validStatuses.Count; i++)
//             {
//                 challenge.Participants.Add(new ChallengeParticipant
//                 {
//                     Id = Guid.NewGuid(),
//                     UserId = Guid.NewGuid(),
//                     Username = $"user{i + 1}",
//                     Status = validStatuses[i],
//                     PersonalContribution = (i + 1) * 10
//                 });
//             }
//
//             var mockSet = CreateMockDbSet(new List<Challenge> { challenge });
//
//             _contextMock.Setup(c => c.Challenges)
//                 .Returns(mockSet.Object);
//
//             var query = new GetChallengeByIdQuery { ChallengeId = challengeId };
//
//             // Act
//             var result = await _handler.Handle(query, CancellationToken.None);
//
//             // Assert
//             result.Should().NotBeNull();
//             result!.Participants.Should().HaveCount(validStatuses.Count);
//
//             // Проверяем, что все статусы корректно отмаппились
//             var mappedStatuses = result.Participants.Select(p => p.Status).ToList();
//             var expectedStatuses = validStatuses.Select(s => s.ToString()).ToList();
//
//             mappedStatuses.Should().BeEquivalentTo(expectedStatuses);
//         }
//     }
// }
