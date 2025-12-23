// Tests/Application/Users/Queries/GetUserByIdQueryHandlerTests.cs (дополнительные тесты)
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Users.Queries;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Users.Queries;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _handler = new GetUserByIdQueryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_UserWithNullInterests_ReturnsEmptyInterestsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            UserInterests = null // null вместо пустого списка
        };

        var queryableUsers = new List<User> { user }.AsQueryable();
        var mockDbSet = CreateMockDbSet(queryableUsers);

        _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Interests.Should().NotBeNull();
        result.Interests.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithNullInterestInUserInterest_HandlesGracefully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            UserInterests = new List<UserInterest>
            {
                new UserInterest { Interest = null }, // null Interest
                new UserInterest { Interest = new Interest { Name = "Valid" } }
            }
        };

        var queryableUsers = new List<User> { user }.AsQueryable();
        var mockDbSet = CreateMockDbSet(queryableUsers);

        _mockContext.Setup(c => c.Users).Returns(mockDbSet.Object);

        var query = new GetUserByIdQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Interests.Should().HaveCount(1);
        result.Interests.Should().Contain("Valid");
        result.Interests.Should().NotContainNulls();
    }

    private Mock<DbSet<User>> CreateMockDbSet(IQueryable<User> queryable)
    {
        var mockDbSet = new Mock<DbSet<User>>();
        mockDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        return mockDbSet;
    }
}
