// SmartPlanner.Tests/Application/Auth/Services/ConfirmationTokenServiceTests.cs
using Xunit;
using Moq;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace SmartPlanner.Tests.Application.Auth.Services
{
    public class ConfirmationTokenServiceTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<ConfirmationTokenService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ConfirmationTokenService _service;

        public ConfirmationTokenServiceTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<ConfirmationTokenService>>();
            _mockConfiguration = new Mock<IConfiguration>();

            _service = new ConfirmationTokenService(
                _mockContext.Object,
                _mockLogger.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task GenerateEmailConfirmationTokenAsync_ValidUser_ReturnsToken()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var mockTokenSet = new Mock<DbSet<EmailConfirmationToken>>();
            _mockContext.Setup(c => c.EmailConfirmationTokens).Returns(mockTokenSet.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var token = await _service.GenerateEmailConfirmationTokenAsync(userId, CancellationToken.None);

            // Assert
            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.True(token.Length > 20); // Base64 token should be long enough
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GeneratePasswordResetTokenAsync_ValidUser_ReturnsToken()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var mockTokenSet = new Mock<DbSet<PasswordResetToken>>();
            _mockContext.Setup(c => c.PasswordResetTokens).Returns(mockTokenSet.Object);

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var token = await _service.GeneratePasswordResetTokenAsync(userId, CancellationToken.None);

            // Assert
            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.True(token.Length > 20);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ValidateEmailConfirmationTokenAsync_ValidToken_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid_token";
            var tokenHash = "hashed_valid_token";

            var confirmationToken = new EmailConfirmationToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsUsed = false
            };

            var mockTokenSet = MockDbSetHelper.CreateMockDbSet(new List<EmailConfirmationToken> { confirmationToken });
            _mockContext.Setup(c => c.EmailConfirmationTokens).Returns(mockTokenSet.Object);

            // Setup hash computation
            var mockService = new Mock<ConfirmationTokenService>(
                _mockContext.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            mockService.Setup(cs => cs.GetType().GetMethod("ComputeSha256Hash",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(cs, new object[] { token }))
                .Returns(tokenHash);

            // Act
            var isValid = await mockService.Object.ValidateEmailConfirmationTokenAsync(
                token, userId, CancellationToken.None);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateEmailConfirmationTokenAsync_ExpiredToken_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "expired_token";
            var tokenHash = "hashed_expired_token";

            var confirmationToken = new EmailConfirmationToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
                IsUsed = false
            };

            var mockTokenSet = MockDbSetHelper.CreateMockDbSet(new List<EmailConfirmationToken> { confirmationToken });
            _mockContext.Setup(c => c.EmailConfirmationTokens).Returns(mockTokenSet.Object);

            // Setup hash computation
            var mockService = new Mock<ConfirmationTokenService>(
                _mockContext.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);

            mockService.Setup(cs => cs.GetType().GetMethod("ComputeSha256Hash",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(cs, new object[] { token }))
                .Returns(tokenHash);

            // Act
            var isValid = await mockService.Object.ValidateEmailConfirmationTokenAsync(
                token, userId, CancellationToken.None);

            // Assert
            Assert.False(isValid);
        }
    }
}
