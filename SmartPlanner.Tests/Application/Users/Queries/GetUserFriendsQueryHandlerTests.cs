// Tests/Application/Users/Queries/GetUserFriendsQueryHandlerTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Users.Queries;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Users.Queries;

public class GetUserFriendsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly GetUserFriendsQueryHandler _handler;

    public GetUserFriendsQueryHandlerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _handler = new GetUserFriendsQueryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_UserHasAcceptedFriends_ReturnsFriendsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friend1Id = Guid.NewGuid();
        var friend2Id = Guid.NewGuid();

        var userFriends = new List<UserFriend>
        {
            new UserFriend { UserId = userId, FriendId = friend1Id, Status = FriendStatus.Accepted },
            new UserFriend { UserId = userId, FriendId = friend2Id, Status = FriendStatus.Accepted },
            new UserFriend { UserId = userId, FriendId = Guid.NewGuid(), Status = FriendStatus.Pending } // Should be excluded
        }.AsQueryable();

        var friends = new List<User>
        {
            new User
            {
                Id = friend1Id,
                Username = "friend1",
                Email = "friend1@example.com",
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { Interest = new Interest { Name = "Sports" } }
                }
            },
            new User
            {
                Id = friend2Id,
                Username = "friend2",
                Email = "friend2@example.com",
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { Interest = new Interest { Name = "Music" } },
                    new UserInterest { Interest = new Interest { Name = "Art" } }
                }
            }
        }.AsQueryable();

        var mockUserFriendsDbSet = new Mock<DbSet<UserFriend>>();
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Provider).Returns(userFriends.Provider);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Expression).Returns(userFriends.Expression);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.ElementType).Returns(userFriends.ElementType);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.GetEnumerator()).Returns(() => userFriends.GetEnumerator());

        var mockUsersDbSet = new Mock<DbSet<User>>();
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(friends.Provider);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(friends.Expression);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(friends.ElementType);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => friends.GetEnumerator());

        _mockContext.Setup(c => c.UserFriends).Returns(mockUserFriendsDbSet.Object);
        _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

        var query = new GetUserFriendsQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(friend1Id);
        result[0].Username.Should().Be("friend1");
        result[0].Interests.Should().Contain("Sports");
        result[1].Id.Should().Be(friend2Id);
        result[1].Username.Should().Be("friend2");
        result[1].Interests.Should().Contain("Music");
        result[1].Interests.Should().Contain("Art");
    }

    [Fact]
    public async Task Handle_UserHasNoFriends_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var emptyUserFriendsList = new List<UserFriend>().AsQueryable();
        var mockUserFriendsDbSet = new Mock<DbSet<UserFriend>>();
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Provider).Returns(emptyUserFriendsList.Provider);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Expression).Returns(emptyUserFriendsList.Expression);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.ElementType).Returns(emptyUserFriendsList.ElementType);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.GetEnumerator()).Returns(() => emptyUserFriendsList.GetEnumerator());

        _mockContext.Setup(c => c.UserFriends).Returns(mockUserFriendsDbSet.Object);

        var query = new GetUserFriendsQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserHasOnlyPendingFriends_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var userFriends = new List<UserFriend>
        {
            new UserFriend { UserId = userId, FriendId = Guid.NewGuid(), Status = FriendStatus.Pending },
            new UserFriend { UserId = userId, FriendId = Guid.NewGuid(), Status = FriendStatus.Pending }
        }.AsQueryable();

        var mockUserFriendsDbSet = new Mock<DbSet<UserFriend>>();
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Provider).Returns(userFriends.Provider);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Expression).Returns(userFriends.Expression);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.ElementType).Returns(userFriends.ElementType);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.GetEnumerator()).Returns(() => userFriends.GetEnumerator());

        _mockContext.Setup(c => c.UserFriends).Returns(mockUserFriendsDbSet.Object);

        var query = new GetUserFriendsQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
