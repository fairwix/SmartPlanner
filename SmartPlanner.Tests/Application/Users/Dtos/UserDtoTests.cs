using SmartPlanner.Application.Users.Dtos;
using Xunit;

namespace SmartPlanner.Application.Tests.Users.Dtos
{
    public class UserDtoTests
    {
        [Fact]
        public void UserDto_Properties_SetCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-10);
            var updatedAt = DateTime.UtcNow;
            var lastLogin = DateTime.UtcNow.AddDays(-1);

            // Act
            var dto = new UserDto(
                Id: id,
                CreatedAt: createdAt,
                UpdatedAt: updatedAt,
                Username: "johndoe",
                Email: "john@example.com",
                Interests: new List<string> { "Programming", "Reading", "Gaming" },
                Balance: 1500,
                StreakCount: 7,
                LastLogin: lastLogin);

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(createdAt, dto.CreatedAt);
            Assert.Equal(updatedAt, dto.UpdatedAt);
            Assert.Equal("johndoe", dto.Username);
            Assert.Equal("john@example.com", dto.Email);
            Assert.Equal(3, dto.Interests.Count);
            Assert.Contains("Programming", dto.Interests);
            Assert.Contains("Reading", dto.Interests);
            Assert.Contains("Gaming", dto.Interests);
            Assert.Equal(1500, dto.Balance);
            Assert.Equal(7, dto.StreakCount);
            Assert.Equal(lastLogin, dto.LastLogin);
        }

        [Fact]
        public void UserDto_WithNullLastLogin_WorksCorrectly()
        {
            // Arrange & Act
            var dto = new UserDto(
                Id: Guid.NewGuid(),
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                Username: "testuser",
                Email: "test@example.com",
                Interests: new List<string>(),
                Balance: 0,
                StreakCount: 0,
                LastLogin: null);

            // Assert
            Assert.Null(dto.LastLogin);
            Assert.Equal("testuser", dto.Username);
            Assert.Empty(dto.Interests);
        }

        [Fact]
        public void UserDto_WithEmptyInterests_WorksCorrectly()
        {
            // Arrange & Act
            var dto = new UserDto(
                Id: Guid.NewGuid(),
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                Username: "emptyuser",
                Email: "empty@example.com",
                Interests: new List<string>(),
                Balance: 100,
                StreakCount: 1,
                LastLogin: DateTime.UtcNow);

            // Assert
            Assert.NotNull(dto.Interests);
            Assert.Empty(dto.Interests);
        }

        [Fact]
        public void CreateUserDto_Properties_SetCorrectly()
        {
            // Arrange & Act
            var dto = new CreateUserDto(
                Username: "newuser",
                Email: "new@example.com",
                Password: "SecurePassword123!",
                Interests: new List<string> { "Sports", "Music" });

            // Assert
            Assert.Equal("newuser", dto.Username);
            Assert.Equal("new@example.com", dto.Email);
            Assert.Equal("SecurePassword123!", dto.Password);
            Assert.Equal(2, dto.Interests.Count);
            Assert.Contains("Sports", dto.Interests);
            Assert.Contains("Music", dto.Interests);
        }

        [Fact]
        public void CreateUserDto_WithNullInterests_WorksCorrectly()
        {
            // Arrange & Act
            var dto = new CreateUserDto(
                Username: "test",
                Email: "test@test.com",
                Password: "password",
                Interests: null);

            // Assert
            Assert.Null(dto.Interests);
        }

        [Fact]
        public void CreateUserDto_WithEmptyInterests_WorksCorrectly()
        {
            // Arrange & Act
            var dto = new CreateUserDto(
                Username: "test",
                Email: "test@test.com",
                Password: "password",
                Interests: new List<string>());

            // Assert
            Assert.NotNull(dto.Interests);
            Assert.Empty(dto.Interests);
        }

        [Fact]
        public void UpdateUserDto_Properties_SetCorrectly()
        {
            // Arrange & Act
            var dto = new UpdateUserDto(
                Username: "updatedusername",
                Interests: new List<string> { "NewInterest1", "NewInterest2" });

            // Assert
            Assert.Equal("updatedusername", dto.Username);
            Assert.Equal(2, dto.Interests.Count);
            Assert.Contains("NewInterest1", dto.Interests);
            Assert.Contains("NewInterest2", dto.Interests);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("usernameonly", null)]
        [InlineData(null, new[] { "Interest1" })]
        [InlineData("", new[] { "Interest1", "Interest2" })]
        public void UpdateUserDto_WithOptionalParameters_WorksCorrectly(
            string? username, string[]? interests)
        {
            // Arrange & Act
            var dto = new UpdateUserDto(
                Username: username,
                Interests: interests?.ToList());

            // Assert
            Assert.Equal(username, dto.Username);

            if (interests == null)
            {
                Assert.Null(dto.Interests);
            }
            else
            {
                Assert.Equal(interests.Length, dto.Interests?.Count);
            }
        }

        [Fact]
        public void UserDto_InheritsFromBaseDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-5);
            var updatedAt = DateTime.UtcNow;

            // Act
            var dto = new UserDto(
                id, createdAt, updatedAt,
                "test", "test@test.com",
                new List<string>(), 0, 0, null);

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(createdAt, dto.CreatedAt);
            Assert.Equal(updatedAt, dto.UpdatedAt);
        }

        [Fact]
        public void UserDto_Equality_WorksForRecords()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow;

            var dto1 = new UserDto(
                id, createdAt, updatedAt,
                "user1", "user1@test.com",
                new List<string> { "Interest1" }, 100, 5, null);

            var dto2 = new UserDto(
                id, createdAt, updatedAt,
                "user1", "user1@test.com",
                new List<string> { "Interest1" }, 100, 5, null);

            var dto3 = new UserDto(
                Guid.NewGuid(), createdAt, updatedAt,
                "user2", "user2@test.com",
                new List<string> { "Interest2" }, 200, 10, null);

            // Assert
            Assert.Equal(dto1, dto2);
            Assert.NotEqual(dto1, dto3);
            Assert.True(dto1 == dto2);
            Assert.False(dto1 == dto3);
        }

        [Fact]
        public void CreateUserDto_WithWhitespaceInInterests_ShouldBeTrimmedByConsumer()
        {
            // Arrange & Act
            var dto = new CreateUserDto(
                Username: "testuser",
                Email: "test@example.com",
                Password: "password",
                Interests: new List<string> { "  Interest1  ", "Interest2  ", "  Interest3" });

            // Assert
            Assert.Equal("  Interest1  ", dto.Interests[0]);
            Assert.Equal("Interest2  ", dto.Interests[1]);
            Assert.Equal("  Interest3", dto.Interests[2]);
        }

        [Fact]
        public void UpdateUserDto_CanUpdateOnlyUsername()
        {
            // Arrange & Act
            var dto = new UpdateUserDto(
                Username: "newusername",
                Interests: null);

            // Assert
            Assert.Equal("newusername", dto.Username);
            Assert.Null(dto.Interests);
        }

        [Fact]
        public void UpdateUserDto_CanUpdateOnlyInterests()
        {
            // Arrange & Act
            var dto = new UpdateUserDto(
                Username: null,
                Interests: new List<string> { "UpdatedInterest" });

            // Assert
            Assert.Null(dto.Username);
            Assert.Single(dto.Interests);
            Assert.Equal("UpdatedInterest", dto.Interests[0]);
        }
    }
}
