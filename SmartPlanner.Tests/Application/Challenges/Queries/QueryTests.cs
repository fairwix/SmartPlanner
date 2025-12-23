// Tests/Unit/Application/Challenges/Queries/QueryTests.cs
using FluentAssertions;
using SmartPlanner.Application.Challenges.Queries;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Challenges.Queries
{
    public class QueryTests
    {
        public class GetChallengesQueryTests
        {
            [Fact]
            public void GetChallengesQuery_ShouldCreateWithDefaultValues()
            {
                // Act
                var query = new GetChallengesQuery();

                // Assert
                query.Should().NotBeNull();
                query.UserId.Should().BeNull();
                query.ActiveOnly.Should().BeFalse();
                query.Type.Should().BeNull();
                query.IsGroupChallenge.Should().BeNull();
            }

            [Fact]
            public void GetChallengesQuery_ShouldCreateWithChallengeType()
            {
                // Arrange
                var challengeType = ChallengeType.StepCount;

                // Act
                var query = new GetChallengesQuery
                {
                    Type = challengeType.ToString()
                };

                // Assert
                query.Type.Should().Be("StepCount");
                Enum.TryParse<ChallengeType>(query.Type, true, out var parsedType).Should().BeTrue();
                parsedType.Should().Be(ChallengeType.StepCount);
            }

            [Theory]
            [InlineData("StepCount")]
            [InlineData("Reading")]
            [InlineData("Exercise")]
            public void GetChallengesQuery_ShouldAcceptValidChallengeTypes(string type)
            {
                // Act
                var query = new GetChallengesQuery { Type = type };

                // Assert
                query.Type.Should().Be(type);

                // Проверяем, что это валидное значение enum
                var canParse = Enum.TryParse<ChallengeType>(type, true, out _);
                canParse.Should().BeTrue();
            }
        }

        public class GetUserChallengesQueryTests
        {
            [Fact]
            public void GetUserChallengesQuery_ShouldCreateWithDefaultValues()
            {
                // Arrange
                var userId = Guid.NewGuid();

                // Act
                var query = new GetUserChallengesQuery { UserId = userId };

                // Assert
                query.Should().NotBeNull();
                query.UserId.Should().Be(userId);
                query.IncludeCompleted.Should().BeFalse();
            }

            [Fact]
            public void GetUserChallengesQuery_ShouldAllowIncludeCompleted()
            {
                // Arrange
                var userId = Guid.NewGuid();

                // Act
                var query = new GetUserChallengesQuery
                {
                    UserId = userId,
                    IncludeCompleted = true
                };

                // Assert
                query.IncludeCompleted.Should().BeTrue();
            }
        }

        public class GetChallengeByIdQueryTests
        {
            [Fact]
            public void GetChallengeByIdQuery_ShouldCreateWithChallengeId()
            {
                // Arrange
                var challengeId = Guid.NewGuid();

                // Act
                var query = new GetChallengeByIdQuery { ChallengeId = challengeId };

                // Assert
                query.Should().NotBeNull();
                query.ChallengeId.Should().Be(challengeId);
            }
        }
    }
}
