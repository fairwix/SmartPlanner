// Tests/Unit/Application/DTOs/Achievement/AchievementDtoTests.cs
using FluentAssertions;
using SmartPlanner.Application.DTOs.Achievement;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.DTOs.Achievement
{
    public class AchievementResponseTests
    {
        [Fact]
        public void AchievementResponse_ShouldCreateSuccessfully()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = "First Goal";
            var description = "Complete your first goal";
            var badgeImage = "badge_first_goal.png";
            var rewardAmount = 50;
            var type = AchievementType.GoalsCompleted;
            var condition = "Complete 1 goal";

            // Act
            var response = new AchievementResponse(
                id,
                name,
                description,
                badgeImage,
                rewardAmount,
                type,
                condition
            );

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().Be(id);
            response.Name.Should().Be(name);
            response.Description.Should().Be(description);
            response.BadgeImage.Should().Be(badgeImage);
            response.RewardAmount.Should().Be(rewardAmount);
            response.Type.Should().Be(type);
            response.Condition.Should().Be(condition);
        }

        [Theory]
        [InlineData(AchievementType.Streak)]
        [InlineData(AchievementType.GoalsCompleted)]
        [InlineData(AchievementType.Friends)]
        [InlineData(AchievementType.ChallengeCompletion)]
        [InlineData(AchievementType.Social)]
        public void AchievementResponse_ShouldHandleAllAchievementTypes(AchievementType type)
        {
            // Act
            var response = new AchievementResponse(
                Guid.NewGuid(),
                "Test Achievement",
                "Test Description",
                "badge.png",
                100,
                type,
                "Test Condition"
            );

            // Assert
            response.Type.Should().Be(type);
        }
    }

    public class UserAchievementResponseTests
    {
        [Fact]
        public void UserAchievementResponse_ShouldCreateSuccessfully()
        {
            // Arrange
            var achievementId = Guid.NewGuid();
            var name = "Consistency Master";
            var description = "Maintain a 30-day streak";
            var badgeImage = "badge_consistency.png";
            var awardedAt = DateTime.UtcNow.AddDays(-5);

            // Act
            var response = new UserAchievementResponse(
                achievementId,
                name,
                description,
                badgeImage,
                awardedAt
            );

            // Assert
            response.Should().NotBeNull();
            response.AchievementId.Should().Be(achievementId);
            response.Name.Should().Be(name);
            response.Description.Should().Be(description);
            response.BadgeImage.Should().Be(badgeImage);
            response.AwardedAt.Should().Be(awardedAt);
        }

        [Fact]
        public void UserAchievementResponse_ShouldHandleRecentAwards()
        {
            // Arrange
            var recentAward = DateTime.UtcNow;

            // Act
            var response = new UserAchievementResponse(
                Guid.NewGuid(),
                "New Achievement",
                "Just earned",
                "badge_new.png",
                recentAward
            );

            // Assert
            response.AwardedAt.Should().Be(recentAward);
        }
    }
}
