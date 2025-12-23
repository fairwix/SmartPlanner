// Tests/Unit/Application/Challenges/Dtos/EnumCompatibilityTests.cs
using FluentAssertions;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Challenges.Dtos
{
    public class EnumCompatibilityTests
    {
        [Fact]
        public void ChallengeDto_Type_ShouldBeCompatibleWithChallengeTypeEnum()
        {
            // Arrange
            var allChallengeTypes = Enum.GetValues(typeof(ChallengeType))
                .Cast<ChallengeType>()
                .Select(ct => ct.ToString())
                .ToList();

            // Act & Assert
            foreach (var typeString in allChallengeTypes)
            {
                // Создаем DTO с типом из enum
                var dto = new ChallengeDto(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    "Test",
                    "Description",
                    typeString,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddDays(1),
                    false,
                    100,
                    50,
                    50.0,
                    true,
                    Guid.NewGuid(),
                    new List<ChallengeParticipantDto>()
                );

                // Проверяем, что можно преобразовать обратно в enum
                var canParse = Enum.TryParse<ChallengeType>(dto.Type, true, out _);
                canParse.Should().BeTrue($"Type '{dto.Type}' should be parsable to ChallengeType enum");
            }
        }

        [Fact]
        public void ChallengeParticipantDto_Status_ShouldBeCompatibleWithParticipantStatusEnum()
        {
            // Arrange
            var allStatuses = Enum.GetValues(typeof(ParticipantStatus))
                .Cast<ParticipantStatus>()
                .Select(ps => ps.ToString())
                .ToList();

            // Act & Assert
            foreach (var statusString in allStatuses)
            {
                // Создаем DTO со статусом из enum
                var dto = new ChallengeParticipantDto(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    Guid.NewGuid(),
                    "testuser",
                    statusString,
                    100,
                    DateTime.UtcNow
                );

                // Проверяем, что можно преобразовать обратно в enum
                var canParse = Enum.TryParse<ParticipantStatus>(dto.Status, true, out _);
                canParse.Should().BeTrue($"Status '{dto.Status}' should be parsable to ParticipantStatus enum");
            }
        }

        [Fact]
        public void CreateChallengeDto_Type_ShouldAcceptAllChallengeTypes()
        {
            // Arrange
            var allChallengeTypes = Enum.GetValues(typeof(ChallengeType))
                .Cast<ChallengeType>()
                .ToList();

            // Act & Assert
            foreach (var challengeType in allChallengeTypes)
            {
                // Создаем DTO с типом из enum
                var dto = new CreateChallengeDto(
                    "Test Challenge",
                    "Description",
                    challengeType.ToString(),
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddDays(30),
                    false,
                    100,
                    Guid.NewGuid()
                );

                // Проверяем, что можно преобразовать обратно в enum
                var canParse = Enum.TryParse<ChallengeType>(dto.Type, true, out var parsedType);
                canParse.Should().BeTrue();
                parsedType.Should().Be(challengeType);
            }
        }

        [Theory]
        [InlineData("InvalidType", false)]
        [InlineData("NotARealType", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void ChallengeDto_ShouldHandleInvalidTypeStrings(string typeString, bool shouldParse)
        {
            // Arrange
            var dto = new ChallengeDto(
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow,
                "Test",
                "Description",
                typeString,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(1),
                false,
                100,
                50,
                50.0,
                true,
                Guid.NewGuid(),
                new List<ChallengeParticipantDto>()
            );

            // Act
            var canParse = Enum.TryParse<ChallengeType>(dto.Type, true, out _);

            // Assert
            canParse.Should().Be(shouldParse);
        }
    }
}
