// Tests/Unit/Application/Challenges/Dtos/ChallengeDtoTests.cs
using FluentAssertions;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Challenges.Dtos
{
    public class ChallengeDtoTests
    {
        [Fact]
        public void ChallengeDto_ShouldCreateSuccessfully()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-5);
            var updatedAt = DateTime.UtcNow;
            var title = "Test Challenge";
            var description = "Test Description";
            var type = ChallengeType.StepCount;
            var startDate = DateTime.UtcNow.AddDays(-2);
            var endDate = DateTime.UtcNow.AddDays(30);
            var isGroupChallenge = true;
            var targetValue = 100;
            var currentValue = 50;
            var groupProgressPercentage = 50.0;
            var isActive = true;
            var createdBy = Guid.NewGuid();
            var participants = new List<ChallengeParticipantDto>();

            // Act
            var dto = new ChallengeDto(
                id,
                createdAt,
                updatedAt,
                title,
                description,
                type.ToString(), // Теперь это string из enum
                startDate,
                endDate,
                isGroupChallenge,
                targetValue,
                currentValue,
                groupProgressPercentage,
                isActive,
                createdBy,
                participants
            );

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(id);
            dto.Title.Should().Be(title);
            dto.Description.Should().Be(description);
            dto.Type.Should().Be(type.ToString());
            dto.IsGroupChallenge.Should().Be(isGroupChallenge);
            dto.TargetValue.Should().Be(targetValue);
            dto.CurrentValue.Should().Be(currentValue);
            dto.GroupProgressPercentage.Should().Be(groupProgressPercentage);
            dto.IsActive.Should().Be(isActive);
            dto.CreatedBy.Should().Be(createdBy);
        }

        [Fact]
        public void ChallengeParticipantDto_ShouldCreateSuccessfully()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-5);
            var updatedAt = DateTime.UtcNow;
            var userId = Guid.NewGuid();
            var username = "testuser";
            var status = ParticipantStatus.Joined.ToString(); // Теперь это string из enum
            var personalContribution = 30;
            var joinedAt = DateTime.UtcNow.AddDays(-4);

            // Act
            var dto = new ChallengeParticipantDto(
                id,
                createdAt,
                updatedAt,
                userId,
                username,
                status,
                personalContribution,
                joinedAt
            );

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(id);
            dto.UserId.Should().Be(userId);
            dto.Username.Should().Be(username);
            dto.Status.Should().Be(status);
            dto.PersonalContribution.Should().Be(personalContribution);
        }

        [Fact]
        public void CreateChallengeDto_ShouldCreateSuccessfully()
        {
            // Arrange
            var title = "New Challenge";
            var description = "New Description";
            var type = ChallengeType.Reading;
            var startDate = DateTime.UtcNow.AddDays(1);
            var endDate = DateTime.UtcNow.AddDays(31);
            var isGroupChallenge = false;
            var targetValue = 50;
            var createdBy = Guid.NewGuid();

            // Act
            var dto = new CreateChallengeDto(
                title,
                description,
                type.ToString(), // Теперь это string из enum
                startDate,
                endDate,
                isGroupChallenge,
                targetValue,
                createdBy
            );

            // Assert
            dto.Should().NotBeNull();
            dto.Title.Should().Be(title);
            dto.Description.Should().Be(description);
            dto.Type.Should().Be(type.ToString());
            dto.IsGroupChallenge.Should().Be(isGroupChallenge);
            dto.TargetValue.Should().Be(targetValue);
            dto.CreatedBy.Should().Be(createdBy);
        }
    }

    public class BaseDtoTests
    {
        [Fact]
        public void BaseDto_ShouldCreateSuccessfully()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-5);
            var updatedAt = DateTime.UtcNow;

            // Act
            var dto = new BaseDto(id, createdAt, updatedAt);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(id);
            dto.CreatedAt.Should().Be(createdAt);
            dto.UpdatedAt.Should().Be(updatedAt);
        }
    }
}
