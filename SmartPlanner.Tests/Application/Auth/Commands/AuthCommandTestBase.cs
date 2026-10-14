// Tests/Application/Auth/Commands/AuthCommandTestBase.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.UnitTests.Auth.Commands;

public abstract class AuthCommandTestBase
{
    protected readonly Mock<IApplicationDbContext> MockContext;
    protected readonly Mock<ITokenService> MockTokenService;
    protected readonly Mock<IPasswordHasher> MockPasswordHasher;
    protected readonly Mock<ILogger> MockLogger;
    protected readonly Mock<IAuditService> MockAuditService;
    protected readonly Mock<IEmailService> MockEmailService;
    protected readonly Mock<IConfirmationTokenService> MockConfirmationTokenService;

    protected AuthCommandTestBase()
    {
        MockContext = new Mock<IApplicationDbContext>();
        MockTokenService = new Mock<ITokenService>();
        MockPasswordHasher = new Mock<IPasswordHasher>();
        MockLogger = new Mock<ILogger>();
        MockAuditService = new Mock<IAuditService>();
        MockEmailService = new Mock<IEmailService>();
        MockConfirmationTokenService = new Mock<IConfirmationTokenService>();
    }

    protected Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        return mockSet;
    }

    protected User CreateTestUser(bool isActive = true, bool isEmailConfirmed = false)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hashed_password",
            PasswordSalt = "salt",
            IsActive = isActive,
            IsDeleted = false,
            IsEmailConfirmed = isEmailConfirmed,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastLoginAt = DateTime.UtcNow.AddHours(-1)
        };
    }
}
