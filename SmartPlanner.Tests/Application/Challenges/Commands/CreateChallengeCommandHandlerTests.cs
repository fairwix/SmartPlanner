// SmartPlanner.Tests/Application/Challenges/Commands/CreateChallengeCommandHandlerTests.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Challenges.Commands;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Tests.Application.Challenges.Commands
{
    public class CreateChallengeCommandHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CreateChallengeCommandHandler>> _mockLogger;
        private readonly CreateChallengeCommandHandler _handler;
        private readonly Guid _testUserId;

        public CreateChallengeCommandHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CreateChallengeCommandHandler>>();

            _testUserId = Guid.NewGuid();

            _handler = new CreateChallengeCommandHandler(_mockContext.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_UserExists_ReturnsChallengeDto()
        {
            // Arrange
            SetupMockContextWithUser(true);

            var command = new CreateChallengeCommand
            {
                Title = "Test Challenge",
                Description = "Test Description",
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(8),
                IsGroupChallenge = true,
                TargetValue = 100,
                CreatedBy = _testUserId
            };

            var expectedChallenge = new Challenge
            {
                Id = Guid.NewGuid(),
                Title = command.Title,
                Description = command.Description,
                Type = ChallengeType.Exercise,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                IsGroupChallenge = command.IsGroupChallenge,
                TargetValue = command.TargetValue,
                CurrentValue = 0,
                CreatedBy = command.CreatedBy
            };

            var expectedDto = new ChallengeDto(
                expectedChallenge.Id,
                expectedChallenge.CreatedAt,
                expectedChallenge.UpdatedAt,
                expectedChallenge.Title,
                expectedChallenge.Description,
                expectedChallenge.Type.ToString(),
                expectedChallenge.StartDate,
                expectedChallenge.EndDate,
                expectedChallenge.IsGroupChallenge,
                expectedChallenge.TargetValue,
                expectedChallenge.CurrentValue,
                0.0, // GroupProgressPercentage
                true, // IsActive
                expectedChallenge.CreatedBy,
                new System.Collections.Generic.List<ChallengeParticipantDto>()
            );

            _mockMapper.Setup(m => m.Map<ChallengeDto>(It.IsAny<Challenge>())).Returns(expectedDto);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(expectedDto.Title);
            result.Description.Should().Be(expectedDto.Description);
            _mockContext.Verify(c => c.Challenges.AddAsync(It.IsAny<Challenge>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsArgumentException()
        {
            // Arrange
            SetupMockContextWithUser(false);

            var command = new CreateChallengeCommand
            {
                Title = "Test Challenge",
                CreatedBy = _testUserId // non-existent user
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _handler.Handle(command, CancellationToken.None));

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsParticipants()
        {
            // Arrange
            SetupMockContextWithUser(true);

            var command = new CreateChallengeCommand
            {
                Title = "Test Challenge",
                Type = "Exercise",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(8),
                TargetValue = 100,
                CreatedBy = _testUserId
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockContext.Verify(c => c.Challenges.AddAsync(It.IsAny<Challenge>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify participant was added
            var challenge = _mockContext.Object.Challenges.First();
            challenge.Participants.Should().NotBeNull();
        }

        private void SetupMockContextWithUser(bool userExists)
        {
            var users = userExists
                ? new List<User> { new User { Id = _testUserId } }
                : new List<User>();

            var mockUsers = MockDbSetHelper.CreateMockDbSet(users);
            var mockChallenges = MockDbSetHelper.CreateMockDbSet(new List<Challenge>());
            var mockParticipants = MockDbSetHelper.CreateMockDbSet(new List<ChallengeParticipant>());

            _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
            _mockContext.Setup(c => c.Challenges).Returns(mockChallenges.Object);
            _mockContext.Setup(c => c.ChallengeParticipants).Returns(mockParticipants.Object);
        }
    }
}
