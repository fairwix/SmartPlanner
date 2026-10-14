using FluentAssertions;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Tests.Domain.Entities
{
    public class UserTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange & Act
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashed_password"
            };

            // Assert
            user.Username.Should().Be("testuser");
            user.Email.Should().Be("test@example.com");
            user.PasswordHash.Should().Be("hashed_password");
            user.Balance.Should().Be(0);
            user.StreakCount.Should().Be(0);
            user.UserInterests.Should().BeEmpty();
            user.Goals.Should().BeEmpty();
            user.Friends.Should().BeEmpty();
        }

        [Fact]
        public void AddReward_ShouldIncreaseBalance()
        {
            // Arrange
            var user = new User { Balance = 100 };
            var rewardAmount = 50;

            // Act
            user.AddReward(rewardAmount);

            // Assert
            user.Balance.Should().Be(150);
        }

        [Theory]
        [InlineData(100, 50, true)]
        [InlineData(30, 50, false)]
        [InlineData(50, 50, true)]
        public void CanAfford_ShouldReturnCorrectResult(int balance, int price, bool expected)
        {
            // Arrange
            var user = new User { Balance = balance };

            // Act
            var result = user.CanAfford(price);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void UpdateStreak_ShouldIncrementStreakCount()
        {
            // Arrange
            var user = new User { StreakCount = 5 };

            // Act
            user.UpdateStreak();

            // Assert
            user.StreakCount.Should().Be(6);
        }

        [Fact]
        public void ResetStreak_ShouldSetStreakToZero()
        {
            // Arrange
            var user = new User { StreakCount = 10 };

            // Act
            user.ResetStreak();

            // Assert
            user.StreakCount.Should().Be(0);
        }

        [Fact]
        public void GetInterests_ShouldReturnInterestNames()
        {
            // Arrange
            var user = new User
            {
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { Interest = new Interest { Name = "Programming" } },
                    new UserInterest { Interest = new Interest { Name = "Sports" } }
                }
            };

            // Act
            var interests = user.GetInterests();

            // Assert
            interests.Should().Contain("Programming");
            interests.Should().Contain("Sports");
            interests.Should().HaveCount(2);
        }

        [Fact]
        public void HasInterest_ShouldReturnTrue_WhenUserHasInterest()
        {
            // Arrange
            var user = new User
            {
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { Interest = new Interest { Name = "Programming" } }
                }
            };

            // Act
            var result = user.HasInterest("Programming");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasInterest_ShouldReturnFalse_WhenUserDoesNotHaveInterest()
        {
            // Arrange
            var user = new User
            {
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { Interest = new Interest { Name = "Sports" } }
                }
            };

            // Act
            var result = user.HasInterest("Programming");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasInterest_ShouldBeCaseInsensitive()
        {
            // Arrange
            var user = new User
            {
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { Interest = new Interest { Name = "Programming" } }
                }
            };

            // Act
            var result = user.HasInterest("PROGRAMMING");

            // Assert
            result.Should().BeTrue();
        }
    }
}
