// // SmartPlanner.Tests/Application/Challenges/Commands/JoinChallengeCommandHandlerTests.cs
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using AutoMapper;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Moq;
// using SmartPlanner.Application.Challenges.Commands;
// using SmartPlanner.Application.Challenges.Dtos;
// using SmartPlanner.Application.Common.Interfaces;
// using SmartPlanner.Domain.Entities;
// using SmartPlanner.Tests.TestHelpers; // Предполагается наличие MockDbSetHelper
// using Xunit;
//
// namespace SmartPlanner.Tests.Application.Challenges.Commands
// {
//     public class JoinChallengeCommandHandlerTests
//     {
//         private readonly Mock<IApplicationDbContext> _mockContext;
//         private readonly Mock<IMapper> _mockMapper;
//         private readonly Mock<ILogger<JoinChallengeCommandHandler>> _mockLogger;
//         private readonly JoinChallengeCommandHandler _handler;
//         private readonly Guid _testUserId;
//         private readonly Guid _testChallengeId;
//
//         public JoinChallengeCommandHandlerTests()
//         {
//             _mockContext = new Mock<IApplicationDbContext>();
//             _mockMapper = new Mock<IMapper>();
//             _mockLogger = new Mock<ILogger<JoinChallengeCommandHandler>>();
//             _handler = new JoinChallengeCommandHandler(_mockContext.Object, _mockMapper.Object, _mockLogger.Object);
//             _testUserId = Guid.NewGuid();
//             _testChallengeId = Guid.NewGuid();
//         }
//
//         [Fact]
//         public async Task Handle_ValidRequest_AddsParticipantAndSaves()
//         {
//             // Arrange
//             var command = new JoinChallengeCommand { ChallengeId = _testChallengeId, UserId = _testUserId };
//             var challenge = new Challenge
//             {
//                 Id = _testChallengeId,
//                 Title = "Test Challenge",
//                 Participants = new List<ChallengeParticipant>() // Пустой список
//             };
//             var user = new User { Id = _testUserId, Username = "testuser" };
//             var expectedParticipant = new ChallengeParticipant { ChallengeId = _testChallengeId, UserId = _testUserId, JoinedAt = DateTime.UtcNow };
//
//             var mockChallenges = MockDbSetHelper.CreateMockDbSet(new List<Challenge> { challenge });
//             var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
//
//             _mockContext.Setup(c => c.Challenges).Returns(mockChallenges.Object);
//             _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));
//
//             var mappedChallengeDto = new ChallengeDto { Id = challenge.Id, Title = challenge.Title, Participants = new List<ChallengeParticipantDto>() };
//             _mockMapper.Setup(m => m.Map<ChallengeDto>(challenge)).Returns(mappedChallengeDto);
//
//             // Act
//             var result = await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal(challenge.Id, result.Id);
//             Assert.Contains(challenge.Participants, p => p.UserId == _testUserId);
//             _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//         }
//
//         [Fact]
//         public async Task Handle_NonExistentUser_ThrowsArgumentException()
//         {
//             // Arrange
//             var command = new JoinChallengeCommand { ChallengeId = _testChallengeId, UserId = _testUserId };
//             var challenge = new Challenge { Id = _testChallengeId, Title = "Test Challenge", Participants = new List<ChallengeParticipant>() };
//
//             var mockChallenges = MockDbSetHelper.CreateMockDbSet(new List<Challenge> { challenge });
//             var emptyUsers = new List<User>().AsQueryable();
//             var mockUsers = MockDbSetHelper.CreateMockDbSet(emptyUsers.ToList());
//
//             _mockContext.Setup(c => c.Challenges).Returns(mockChallenges.Object);
//             _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
//         }
//
//         [Fact]
//         public async Task Handle_NonExistentChallenge_ThrowsArgumentException()
//         {
//             // Arrange
//             var command = new JoinChallengeCommand { ChallengeId = _testChallengeId, UserId = _testUserId };
//             var user = new User { Id = _testUserId, Username = "testuser" };
//
//             var emptyChallenges = new List<Challenge>().AsQueryable();
//             var mockChallenges = MockDbSetHelper.CreateMockDbSet(emptyChallenges.ToList());
//             var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
//
//             _mockContext.Setup(c => c.Challenges).Returns(mockChallenges.Object);
//             _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
//
//             // Act & Assert
//             await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
//         }
//
//         [Fact]
//         public async Task Handle_AlreadyParticipating_ThrowsInvalidOperationException()
//         {
//             // Arrange
//             var command = new JoinChallengeCommand { ChallengeId = _testChallengeId, UserId = _testUserId };
//             var existingParticipant = new ChallengeParticipant { ChallengeId = _testChallengeId, UserId = _testUserId, JoinedAt = DateTime.UtcNow };
//             var challenge = new Challenge
//             {
//                 Id = _testChallengeId,
//                 Title = "Test Challenge",
//                 Participants = new List<ChallengeParticipant> { existingParticipant } // Уже участник
//             };
//             var user = new User { Id = _testUserId, Username = "testuser" };
//
//             var mockChallenges = MockDbSetHelper.CreateMockDbSet(new List<Challenge> { challenge });
//             var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
//
//             _mockContext.Setup(c => c.Challenges).Returns(mockChallenges.Object);
//             _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
//
//             // Act & Assert
//             await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
//         }
//
//         [Fact]
//         public async Task Handle_ValidRequest_LogsInformation()
//         {
//             // Arrange
//             var command = new JoinChallengeCommand { ChallengeId = _testChallengeId, UserId = _testUserId };
//             var challenge = new Challenge { Id = _testChallengeId, Title = "Log Test Challenge", Participants = new List<ChallengeParticipant>() };
//             var user = new User { Id = _testUserId, Username = "testuser" };
//
//             var mockChallenges = MockDbSetHelper.CreateMockDbSet(new List<Challenge> { challenge });
//             var mockUsers = MockDbSetHelper.CreateMockDbSet(new List<User> { user });
//
//             _mockContext.Setup(c => c.Challenges).Returns(mockChallenges.Object);
//             _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
//             _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));
//
//             var mappedChallengeDto = new ChallengeDto { Id = challenge.Id };
//             _mockMapper.Setup(m => m.Map<ChallengeDto>(challenge)).Returns(mappedChallengeDto);
//
//             // Act
//             await _handler.Handle(command, CancellationToken.None);
//
//             // Assert
//             _mockLogger.Verify(
//                 x => x.Log(
//                     LogLevel.Information,
//                     It.IsAny<EventId>(),
//                     It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("joining challenge")),
//                     It.IsAny<Exception?>(),
//                     It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
//                 Times.Once);
//
//             _mockLogger.Verify(
//                 x => x.Log(
//                     LogLevel.Information,
//                     It.IsAny<EventId>(),
//                     It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("successfully joined challenge")),
//                     It.IsAny<Exception?>(),
//                     It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
//                 Times.Once);
//         }
//     }
// }
