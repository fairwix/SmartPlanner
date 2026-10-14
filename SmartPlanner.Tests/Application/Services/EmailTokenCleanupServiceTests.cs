// Tests/Application/Services/EmailTokenCleanupServiceTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Services;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Services;

public class EmailTokenCleanupServiceTests
{
    private readonly Mock<ILogger<EmailTokenCleanupService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public EmailTokenCleanupServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailTokenCleanupService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _cancellationTokenSource = new CancellationTokenSource();

        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);

        _mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(_mockServiceScope.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CleansUpExpiredEmailTokens()
    {
        // Arrange
        var mockContext = new Mock<IApplicationDbContext>();
        var expiredToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false
        };

        var tokens = new List<EmailConfirmationToken> { expiredToken };
        var mockSet = CreateMockDbSet(tokens);

        mockContext.Setup(c => c.EmailConfirmationTokens).Returns(mockSet.Object);
        mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IApplicationDbContext)))
            .Returns(mockContext.Object);

        var service = new EmailTokenCleanupService(_mockLogger.Object, _mockServiceProvider.Object);

        // Act - запускаем и быстро отменяем
        var task = service.StartAsync(_cancellationTokenSource.Token);

        // Даем немного времени на выполнение
        await Task.Delay(100);
        await _cancellationTokenSource.CancelAsync();

        // Ждем завершения
        try { await task; } catch { /* Ignore cancellation */ }

        // Assert
        mockContext.Verify(c => c.EmailConfirmationTokens.RemoveRange(
            It.Is<IEnumerable<EmailConfirmationToken>>(t => t.Contains(expiredToken))), Times.AtLeastOnce);
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_CleansUpUsedEmailTokens()
    {
        // Arrange
        var mockContext = new Mock<IApplicationDbContext>();
        var usedToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Not expired yet
            IsUsed = true // But used
        };

        var tokens = new List<EmailConfirmationToken> { usedToken };
        var mockSet = CreateMockDbSet(tokens);

        mockContext.Setup(c => c.EmailConfirmationTokens).Returns(mockSet.Object);
        mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IApplicationDbContext)))
            .Returns(mockContext.Object);

        var service = new EmailTokenCleanupService(_mockLogger.Object, _mockServiceProvider.Object);

        // Act
        var task = service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(100);
        await _cancellationTokenSource.CancelAsync();
        try { await task; } catch { }

        // Assert
        mockContext.Verify(c => c.EmailConfirmationTokens.RemoveRange(
            It.Is<IEnumerable<EmailConfirmationToken>>(t => t.Contains(usedToken))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_CleansUpExpiredPasswordResetTokens()
    {
        // Arrange
        var mockContext = new Mock<IApplicationDbContext>();
        var expiredToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false
        };

        var tokens = new List<PasswordResetToken> { expiredToken };
        var mockSet = CreateMockDbSet(tokens);

        mockContext.Setup(c => c.PasswordResetTokens).Returns(mockSet.Object);
        mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IApplicationDbContext)))
            .Returns(mockContext.Object);

        var service = new EmailTokenCleanupService(_mockLogger.Object, _mockServiceProvider.Object);

        // Act
        var task = service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(100);
        await _cancellationTokenSource.CancelAsync();
        try { await task; } catch { }

        // Assert
        mockContext.Verify(c => c.PasswordResetTokens.RemoveRange(
            It.Is<IEnumerable<PasswordResetToken>>(t => t.Contains(expiredToken))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_NoExpiredTokens_DoesNothing()
    {
        // Arrange
        var mockContext = new Mock<IApplicationDbContext>();
        var validToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Not expired
            IsUsed = false // Not used
        };

        var tokens = new List<EmailConfirmationToken> { validToken };
        var mockSet = CreateMockDbSet(tokens);

        mockContext.Setup(c => c.EmailConfirmationTokens).Returns(mockSet.Object);

        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IApplicationDbContext)))
            .Returns(mockContext.Object);

        var service = new EmailTokenCleanupService(_mockLogger.Object, _mockServiceProvider.Object);

        // Act
        var task = service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(100);
        await _cancellationTokenSource.CancelAsync();
        try { await task; } catch { }

        // Assert
        mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExceptionOccurs_LogsErrorAndRetries()
    {
        // Arrange
        var mockContext = new Mock<IApplicationDbContext>();
        mockContext.Setup(c => c.EmailConfirmationTokens)
            .Throws(new Exception("Database error"));

        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IApplicationDbContext)))
            .Returns(mockContext.Object);

        var service = new EmailTokenCleanupService(_mockLogger.Object, _mockServiceProvider.Object);

        // Act
        var task = service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(100);
        await _cancellationTokenSource.CancelAsync();
        try { await task; } catch { }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error cleaning up email tokens")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        return mockSet;
    }
}
