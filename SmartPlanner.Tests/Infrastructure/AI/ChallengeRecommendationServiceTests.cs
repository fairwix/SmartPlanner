// SmartPlanner.Tests/Infrastructure/AI/ChallengeRecommendationServiceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.AI.Queries;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.AI;
using Xunit;
using FluentAssertions;

namespace SmartPlanner.Tests.Infrastructure.AI
{
    public class ChallengeRecommendationServiceTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly ChallengeRecommendationService _service;
        private readonly Guid _testUserId;

        public ChallengeRecommendationServiceTests()
        {
            _mockMediator = new Mock<IMediator>();
            _service = new ChallengeRecommendationService(_mockMediator.Object);
            _testUserId = Guid.NewGuid();
        }

        [Fact]
        public async Task GenerateSmartChallengesAsync_ValidUserId_ReturnsChallenges()
        {
            // Arrange
            var expectedChallenges = new List<ChallengeDto>
            {
                new ChallengeDto(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    "Fitness Challenge",
                    "Complete 7 workouts",
                    "Exercise",
                    DateTime.UtcNow.AddDays(1),
                    DateTime.UtcNow.AddDays(8),
                    true,
                    7,
                    0,
                    0.0,
                    true,
                    _testUserId,
                    new List<ChallengeParticipantDto>()
                ),
                new ChallengeDto(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    "Reading Challenge",
                    "Read 3 books",
                    "Reading",
                    DateTime.UtcNow.AddDays(1),
                    DateTime.UtcNow.AddDays(15),
                    false,
                    3,
                    0,
                    0.0,
                    true,
                    _testUserId,
                    new List<ChallengeParticipantDto>()
                )
            };

            _mockMediator.Setup(m => m.Send(It.IsAny<GeneratePersonalChallengesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedChallenges);

            // Act
            var result = await _service.GenerateSmartChallengesAsync(_testUserId, 2);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Title.Should().Be("Fitness Challenge");
            result.Last().Title.Should().Be("Reading Challenge");

            _mockMediator.Verify(m => m.Send(
                It.Is<GeneratePersonalChallengesQuery>(q =>
                    q.UserId == _testUserId && q.Count == 2),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task GenerateSmartChallengesAsync_DefaultCount_ReturnsThreeChallenges()
        {
            // Arrange
            var expectedChallenges = Enumerable.Range(1, 3).Select(i =>
                new ChallengeDto(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    $"Challenge {i}",
                    $"Description {i}",
                    "Custom",
                    DateTime.UtcNow.AddDays(1),
                    DateTime.UtcNow.AddDays(8),
                    false,
                    10,
                    0,
                    0.0,
                    true,
                    _testUserId,
                    new List<ChallengeParticipantDto>()
                )
            ).ToList();

            _mockMediator.Setup(m => m.Send(It.IsAny<GeneratePersonalChallengesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedChallenges);

            // Act
            var result = await _service.GenerateSmartChallengesAsync(_testUserId);

            // Assert
            result.Should().HaveCount(3);
        }
    }
}
