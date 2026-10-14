// Tests/Application/Users/Commands/AddFriendCommandHandlerTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Users.Commands;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Users.Commands;

public class AddFriendCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<DbSet<User>> _mockUsersSet;
    private readonly Mock<DbSet<UserFriend>> _mockUserFriendsSet;
    private readonly AddFriendCommandHandler _handler;

    public AddFriendCommandHandlerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockUsersSet = new Mock<DbSet<User>>();
        _mockUserFriendsSet = new Mock<DbSet<UserFriend>>();

        _mockContext.Setup(c => c.Users).Returns(_mockUsersSet.Object);
        _mockContext.Setup(c => c.UserFriends).Returns(_mockUserFriendsSet.Object);

        _handler = new AddFriendCommandHandler(_mockContext.Object);
    }

    [Fact]
public async Task Handle_UsersExistAndNotFriends_ReturnsTrueAndAddsFriendship()
{
    // Arrange
    var userId = Guid.NewGuid();
    var friendId = Guid.NewGuid();

    var mockUsersDbSet = SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateMockDbSet(new List<User>
    {
        new User { Id = userId, Username = "user1" },
        new User { Id = friendId, Username = "user2" }
    });
    var mockUserFriendsDbSet = SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateMockDbSet(new List<UserFriend>());

    _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);
    _mockContext.Setup(c => c.UserFriends).Returns(mockUserFriendsDbSet.Object);
    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    UserFriend capturedFriend = null;
    mockUserFriendsDbSet.Setup(s => s.AddAsync(It.IsAny<UserFriend>(), It.IsAny<CancellationToken>()))
        .Callback<UserFriend, CancellationToken>((userFriend, _) => capturedFriend = userFriend)
        .Returns((UserFriend _, CancellationToken _) => ValueTask.FromResult<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserFriend>>(null!));

    var command = new AddFriendCommand { UserId = userId, FriendId = friendId };

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeTrue();
    capturedFriend.Should().NotBeNull();
    capturedFriend!.UserId.Should().Be(userId);
    capturedFriend.FriendId.Should().Be(friendId);
    capturedFriend.Status.Should().Be(FriendStatus.Pending);
    _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

    [Fact]
    public async Task Handle_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();

        var emptyUsersList = new List<User>().AsQueryable();
        var mockUsersDbSet = new Mock<DbSet<User>>();
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateAsyncQueryProvider(emptyUsersList));
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(emptyUsersList.Expression);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(emptyUsersList.ElementType);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => emptyUsersList.GetEnumerator());

        _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

        var command = new AddFriendCommand { UserId = userId, FriendId = friendId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FriendDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();

        var users = new List<User>
        {
            new User { Id = userId, Username = "user1" }
        }.AsQueryable();

        var mockUsersDbSet = new Mock<DbSet<User>>();
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateAsyncQueryProvider(users));
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());

        _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

        var command = new AddFriendCommand { UserId = userId, FriendId = friendId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyFriends_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();

        var users = new List<User>
        {
            new User { Id = userId, Username = "user1" },
            new User { Id = friendId, Username = "user2" }
        }.AsQueryable();

        var userFriends = new List<UserFriend>
        {
            new UserFriend { UserId = userId, FriendId = friendId, Status = FriendStatus.Pending }
        }.AsQueryable();

        var mockUsersDbSet = new Mock<DbSet<User>>();
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateAsyncQueryProvider(users));
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
        mockUsersDbSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(() => users.GetEnumerator());

        var mockUserFriendsDbSet = new Mock<DbSet<UserFriend>>();
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Provider).Returns(SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateAsyncQueryProvider(userFriends));
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.Expression).Returns(userFriends.Expression);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.ElementType).Returns(userFriends.ElementType);
        mockUserFriendsDbSet.As<IQueryable<UserFriend>>().Setup(m => m.GetEnumerator()).Returns(() => userFriends.GetEnumerator());

        _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);
        _mockContext.Setup(c => c.UserFriends).Returns(mockUserFriendsDbSet.Object);

        var command = new AddFriendCommand { UserId = userId, FriendId = friendId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
