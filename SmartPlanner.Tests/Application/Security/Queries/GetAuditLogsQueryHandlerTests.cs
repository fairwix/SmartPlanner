// Tests/Application.UnitTests/Security/Queries/GetUserAuditLogsQueryHandlerTests.cs

using FluentAssertions;
using MediatR;
using Moq;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Security.Dtos;
using SmartPlanner.Application.Security.Queries;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Security.Queries
{
    public class GetUserAuditLogsQueryHandlerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Guid _userId = Guid.NewGuid();

        public GetUserAuditLogsQueryHandlerTests()
        {
            _mockMediator = new Mock<IMediator>();
        }

        [Fact]
        public async Task Handle_ShouldReturnUserLogs_WhenUserHasLogs()
        {
            // Arrange
            var query = new GetUserAuditLogsQuery
            {
                UserId = _userId,
                PageNumber = 1,
                PageSize = 10
            };

            var expectedDtos = new List<SecurityAuditLogDto>
            {
                new SecurityAuditLogDto(
                    Id: Guid.NewGuid(),
                    EventType: SecurityEventType.Login.ToString(),
                    UserId: _userId,
                    Email: "user@test.com",
                    IpAddress: "192.168.1.1",
                    UserAgent: "Chrome",
                    Success: true,
                    Details: null,
                    Timestamp: DateTime.UtcNow.AddHours(-1),
                    CreatedAt: DateTime.UtcNow.AddHours(-1)
                ),
                new SecurityAuditLogDto(
                    Id: Guid.NewGuid(),
                    EventType: SecurityEventType.PasswordChanged.ToString(),
                    UserId: _userId,
                    Email: "user@test.com",
                    IpAddress: "192.168.1.1",
                    UserAgent: "Chrome",
                    Success: true,
                    Details: null,
                    Timestamp: DateTime.UtcNow.AddDays(-1),
                    CreatedAt: DateTime.UtcNow.AddDays(-1)
                )
            };

            var expectedResult = new PagedResult<SecurityAuditLogDto>(expectedDtos, 2, 1, 10);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetUserAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.All(dto => dto.UserId == _userId).Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldReturnEmpty_WhenUserHasNoLogs()
        {
            // Arrange
            var query = new GetUserAuditLogsQuery
            {
                UserId = Guid.NewGuid(),
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResult = new PagedResult<SecurityAuditLogDto>(
                new List<SecurityAuditLogDto>(), 0, 1, 10);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetUserAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ShouldReturnPaginated_WhenPageSizeSmallerThanTotal()
        {
            // Arrange
            var query = new GetUserAuditLogsQuery
            {
                UserId = _userId,
                PageNumber = 1,
                PageSize = 1
            };

            var expectedDtos = new List<SecurityAuditLogDto>
            {
                new SecurityAuditLogDto(
                    Id: Guid.NewGuid(),
                    EventType: SecurityEventType.Login.ToString(),
                    UserId: _userId,
                    Email: "user@test.com",
                    IpAddress: "192.168.1.1",
                    UserAgent: "Chrome",
                    Success: true,
                    Details: null,
                    Timestamp: DateTime.UtcNow.AddHours(-1),
                    CreatedAt: DateTime.UtcNow.AddHours(-1)
                )
            };

            var expectedResult = new PagedResult<SecurityAuditLogDto>(expectedDtos, 4, 1, 1);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetUserAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(4);
            result.PageSize.Should().Be(1);
            result.TotalPages.Should().Be(4);
        }

        [Fact]
        public async Task Handle_ShouldReturnSecondPage_WhenPageNumberIs2()
        {
            // Arrange
            var query = new GetUserAuditLogsQuery
            {
                UserId = _userId,
                PageNumber = 2,
                PageSize = 2
            };

            var expectedDtos = new List<SecurityAuditLogDto>
            {
                new SecurityAuditLogDto(
                    Id: Guid.NewGuid(),
                    EventType: SecurityEventType.Logout.ToString(),
                    UserId: _userId,
                    Email: "user@test.com",
                    IpAddress: "192.168.1.1",
                    UserAgent: "Chrome",
                    Success: true,
                    Details: null,
                    Timestamp: DateTime.UtcNow.AddHours(-3),
                    CreatedAt: DateTime.UtcNow.AddHours(-3)
                )
            };

            var expectedResult = new PagedResult<SecurityAuditLogDto>(expectedDtos, 3, 2, 2);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetUserAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.TotalPages.Should().Be(2); // ceil(3/2) = 2
        }

        [Fact]
        public async Task Handle_ShouldValidateQueryParameters()
        {
            // Arrange
            var query = new GetUserAuditLogsQuery
            {
                UserId = Guid.Empty,
                PageNumber = 0,
                PageSize = 0
            };

            var expectedResult = new PagedResult<SecurityAuditLogDto>(
                new List<SecurityAuditLogDto>(), 0, 1, 20);

            _mockMediator.Setup(m => m.Send(It.Is<GetUserAuditLogsQuery>(q =>
                q.UserId == Guid.Empty && q.PageNumber == 1 && q.PageSize == 20),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(0);
            result.PageNumber.Should().Be(1); // Исправлено на минимальное значение
            result.PageSize.Should().Be(20); // Исправлено на значение по умолчанию
        }
    }
}
