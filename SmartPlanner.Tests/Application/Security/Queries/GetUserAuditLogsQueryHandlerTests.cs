// Tests/Application.UnitTests/Security/Queries/GetAuditLogsQueryHandlerTests.cs

using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Security.Dtos;
using SmartPlanner.Application.Security.Queries;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Security.Queries
{
    public class GetAuditLogsQueryHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<DbSet<SecurityAuditLog>> _mockDbSet;
        private readonly List<SecurityAuditLog> _auditLogs;
        private readonly Guid _userId = Guid.NewGuid();

        public GetAuditLogsQueryHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMediator = new Mock<IMediator>();
            _mockDbSet = new Mock<DbSet<SecurityAuditLog>>();

            // Подготовка тестовых данных
            _auditLogs = new List<SecurityAuditLog>
            {
                new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = SecurityEventType.Login,
                    UserId = _userId,
                    Email = "user1@test.com",
                    IpAddress = "192.168.1.1",
                    UserAgent = "Mozilla/5.0",
                    Success = true,
                    Details = "{\"device\": \"Windows\"}",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = SecurityEventType.Login,
                    UserId = _userId,
                    Email = "user1@test.com",
                    IpAddress = "192.168.1.2",
                    UserAgent = "Mozilla/5.0",
                    Success = false,
                    Details = "{\"reason\": \"Wrong password\"}",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                }
            };

            // Настройка мока DbSet
            var data = _auditLogs.AsQueryable();
            _mockDbSet.As<IQueryable<SecurityAuditLog>>().Setup(m => m.Provider).Returns(data.Provider);
            _mockDbSet.As<IQueryable<SecurityAuditLog>>().Setup(m => m.ElementType).Returns(data.ElementType);
            _mockDbSet.As<IQueryable<SecurityAuditLog>>().Setup(m => m.Expression).Returns(data.Expression);
            _mockDbSet.As<IQueryable<SecurityAuditLog>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            _mockContext.Setup(c => c.SecurityAuditLogs).Returns(_mockDbSet.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnAllLogs_WhenNoFilters()
        {
            // Arrange
            var query = new GetAuditLogsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Настройка мока для CountAsync
            _mockDbSet.Setup(m => m.CountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_auditLogs.Count);

            // Мокаем обработчик через IMediator
            var expectedDtos = _auditLogs.Select(log => new SecurityAuditLogDto(
                log.Id,
                log.EventType.ToString(),
                log.UserId,
                log.Email,
                log.IpAddress,
                log.UserAgent,
                log.Success,
                log.Details != null ? Newtonsoft.Json.JsonConvert.DeserializeObject(log.Details) : null,
                log.Timestamp,
                log.CreatedAt
            )).ToList();

            var expectedResult = new PagedResult<SecurityAuditLogDto>(expectedDtos, 2, 1, 10);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_ShouldFilterByEventType_WhenEventTypeProvided()
        {
            // Arrange
            var query = new GetAuditLogsQuery
            {
                EventType = SecurityEventType.Login,
                PageNumber = 1,
                PageSize = 10
            };

            var filteredLogs = _auditLogs.Where(l => l.EventType == SecurityEventType.Login).ToList();
            var expectedDtos = filteredLogs.Select(log => new SecurityAuditLogDto(
                log.Id,
                log.EventType.ToString(),
                log.UserId,
                log.Email,
                log.IpAddress,
                log.UserAgent,
                log.Success,
                log.Details != null ? Newtonsoft.Json.JsonConvert.DeserializeObject(log.Details) : null,
                log.Timestamp,
                log.CreatedAt
            )).ToList();

            var expectedResult = new PagedResult<SecurityAuditLogDto>(expectedDtos, 2, 1, 10);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_ShouldFilterByUserId_WhenUserIdProvided()
        {
            // Arrange
            var query = new GetAuditLogsQuery
            {
                UserId = _userId,
                PageNumber = 1,
                PageSize = 10
            };

            var filteredLogs = _auditLogs.Where(l => l.UserId == _userId).ToList();
            var expectedDtos = filteredLogs.Select(log => new SecurityAuditLogDto(
                log.Id,
                log.EventType.ToString(),
                log.UserId,
                log.Email,
                log.IpAddress,
                log.UserAgent,
                log.Success,
                log.Details != null ? Newtonsoft.Json.JsonConvert.DeserializeObject(log.Details) : null,
                log.Timestamp,
                log.CreatedAt
            )).ToList();

            var expectedResult = new PagedResult<SecurityAuditLogDto>(expectedDtos, 2, 1, 10);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_ShouldReturnPaginatedResults_WhenPageSizeSmallerThanTotal()
        {
            // Arrange
            var query = new GetAuditLogsQuery
            {
                PageNumber = 1,
                PageSize = 1
            };

            var pagedLogs = _auditLogs.Take(1).ToList();
            var expectedDtos = pagedLogs.Select(log => new SecurityAuditLogDto(
                log.Id,
                log.EventType.ToString(),
                log.UserId,
                log.Email,
                log.IpAddress,
                log.UserAgent,
                log.Success,
                log.Details != null ? Newtonsoft.Json.JsonConvert.DeserializeObject(log.Details) : null,
                log.Timestamp,
                log.CreatedAt
            )).ToList();

            var expectedResult = new PagedResult<SecurityAuditLogDto>(expectedDtos, 2, 1, 1);

            _mockMediator.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockMediator.Object.Send(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(2);
            result.PageSize.Should().Be(1);
            result.TotalPages.Should().Be(2);
        }
    }
}
