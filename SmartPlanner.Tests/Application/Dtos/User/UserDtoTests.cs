// Tests/Unit/Application/DTOs/User/UserDtoTests.cs
using FluentAssertions;
using SmartPlanner.Application.DTOs.User;
using Xunit;

namespace SmartPlanner.Application.UnitTests.DTOs.User
{
    public class CreateUserRequestTests
    {
        [Fact]
        public void CreateUserRequest_ShouldInitializeLists()
        {
            // Act
            var request = new CreateUserRequest();

            // Assert
            request.Should().NotBeNull();
            request.Interests.Should().NotBeNull();
            request.Interests.Should().BeEmpty();
        }

        [Fact]
        public void CreateUserRequest_ShouldCreateWithValues()
        {
            // Arrange
            var username = "testuser";
            var email = "test@example.com";
            var password = "password123";
            var interests = new List<string> { "Sports", "Reading", "Technology" };

            // Act
            var request = new CreateUserRequest
            {
                Username = username,
                Email = email,
                Password = password,
                Interests = interests
            };

            // Assert
            request.Username.Should().Be(username);
            request.Email.Should().Be(email);
            request.Password.Should().Be(password);
            request.Interests.Should().BeEquivalentTo(interests);
        }

        [Fact]
        public void CreateUserRequest_ShouldAllowEmptyInterests()
        {
            // Act
            var request = new CreateUserRequest
            {
                Username = "test",
                Email = "test@example.com",
                Password = "password",
                Interests = new List<string>() // Пустой список
            };

            // Assert
            request.Interests.Should().BeEmpty();
        }
    }

    public class UpdateUserRequestTests
    {
        [Fact]
        public void UpdateUserRequest_ShouldAllowNullProperties()
        {
            // Act
            var request = new UpdateUserRequest
            {
                Username = null,
                Interests = null
            };

            // Assert
            request.Username.Should().BeNull();
            request.Interests.Should().BeNull();
        }

        [Fact]
        public void UpdateUserRequest_ShouldUpdateUsername()
        {
            // Arrange
            var newUsername = "newusername";

            // Act
            var request = new UpdateUserRequest
            {
                Username = newUsername
            };

            // Assert
            request.Username.Should().Be(newUsername);
        }

        [Fact]
        public void UpdateUserRequest_ShouldUpdateInterests()
        {
            // Arrange
            var newInterests = new List<string> { "Coding", "Gaming" };

            // Act
            var request = new UpdateUserRequest
            {
                Interests = newInterests
            };

            // Assert
            request.Interests.Should().BeEquivalentTo(newInterests);
        }
    }

    public class UserResponseTests
    {
        [Fact]
        public void UserResponse_ShouldInitializeLists()
        {
            // Act
            var response = new UserResponse();

            // Assert
            response.Should().NotBeNull();
            response.Interests.Should().NotBeNull();
            response.Interests.Should().BeEmpty();
        }

        [Fact]
        public void UserResponse_ShouldCreateWithValues()
        {
            // Arrange
            var id = Guid.NewGuid();
            var username = "john_doe";
            var email = "john@example.com";
            var interests = new List<string> { "Sports", "Music" };
            var balance = 1000;
            var streakCount = 7;
            var createdAt = DateTime.UtcNow.AddDays(-30);
            var lastLogin = DateTime.UtcNow.AddHours(-2);

            // Act
            var response = new UserResponse
            {
                Id = id,
                Username = username,
                Email = email,
                Interests = interests,
                Balance = balance,
                StreakCount = streakCount,
                CreatedAt = createdAt,
                LastLogin = lastLogin
            };

            // Assert
            response.Id.Should().Be(id);
            response.Username.Should().Be(username);
            response.Email.Should().Be(email);
            response.Interests.Should().BeEquivalentTo(interests);
            response.Balance.Should().Be(balance);
            response.StreakCount.Should().Be(streakCount);
            response.CreatedAt.Should().Be(createdAt);
            response.LastLogin.Should().Be(lastLogin);
        }

        [Fact]
        public void UserResponse_ShouldAllowZeroValues()
        {
            // Act
            var response = new UserResponse
            {
                Id = Guid.NewGuid(),
                Username = "test",
                Email = "test@example.com",
                Balance = 0,
                StreakCount = 0,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };

            // Assert
            response.Balance.Should().Be(0);
            response.StreakCount.Should().Be(0);
        }
    }
}
