// // SmartPlanner.Tests/Application/Security/Services/AuditServiceTests.cs
// using Xunit;
// using Moq;
// using SmartPlanner.Application.Security.Services;
// using SmartPlanner.Application.Common.Interfaces;
// using Microsoft.EntityFrameworkCore;
// using SmartPlanner.Domain.Entities;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.DependencyInjection;
// using System;
// using System.Collections.Generic;
//
// namespace SmartPlanner.Tests.Application.Security.Services
// {
//     public class AuditServiceTests
//     {
//         private readonly Mock<IApplicationDbContext> _mockContext;
//         private readonly Mock<ILogger<AuditService>> _mockLogger;
//         private readonly Mock<IServiceProvider> _mockServiceProvider;
//         private readonly AuditService _service;
//
//         public AuditServiceTests()
//         {
//             _mockContext = new Mock<IApplicationDbContext>();
//             _mockLogger = new Mock<ILogger<AuditService>>();
//             _mockServiceProvider = new Mock<IServiceProvider>();
//
//             _service = new AuditService(
//                 _mockContext.Object,
//                 _mockLogger.Object,
//                 _mockServiceProvider.Object
//             );
//         }
//
//         [Fact]
//         public async Task LogSecurityEventAsync_ValidEvent_SavesToDatabase()
//         {
//             // Arrange
//             var mockSecurityLogs = new Mock<DbSet<SecurityAuditLog>>();
//             _mockContext.Setup(c => c.SecurityAuditLogs).Returns(mockSecurityLogs.Object);
//
//             var mockScopedContext = new Mock<IApplicationDbContext>();
//             var mockScopedSecurityLogs = new Mock<DbSet<SecurityAuditLog>>();
//             mockScopedContext.Setup(c => c.SecurityAuditLogs).Returns(mockScopedSecurityLogs.Object);
//             mockScopedContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
//
//             _mockServiceProvider.Setup(sp => sp.CreateScope())
//                 .Returns(() => {
//                     var scope = new Mock<IServiceScope>();
//                     scope.SetupGet(s => s.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
//                     scope.Setup(s => s.ServiceProvider.GetService(typeof(IApplicationDbContext)))
//                         .Returns(mockScopedContext.Object);
//                     return scope.Object;
//                 });
//
//             // Act
//             await _service.LogSecurityEventAsync(
//                 SecurityEventType.Login,
//                 Guid.NewGuid(),
//                 "test@example.com",
//                 "192.168.1.1",
//                 "TestAgent",
//                 true,
//                 new { Test = "Data" },
//                 CancellationToken.None
//             );
//
//             // Assert
//             _mockLogger.Verify(l => l.LogDebug(It.IsAny<string>(), It.IsAny<object[]>()),
//                 Times.Once);
//         }
//
//         [Fact]
//         public async Task GetFailedLoginCountAsync_ReturnsCorrectCount()
//         {
//             // Arrange
//             var ipAddress = "192.168.1.1";
//             var since = DateTime.UtcNow.AddMinutes(-5);
//
//             var mockLogs = new List<SecurityAuditLog>
//             {
//                 new SecurityAuditLog { EventType = SecurityEventType.FailedLogin, IpAddress = ipAddress, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
//                 new SecurityAuditLog { EventType = SecurityEventType.FailedLogin, IpAddress = ipAddress, Timestamp = DateTime.UtcNow.AddMinutes(-2) },
//                 new SecurityAuditLog { EventType = SecurityEventType.FailedLogin, IpAddress = "192.168.1.2", Timestamp = DateTime.UtcNow.AddMinutes(-1) }
//             };
//
//             var mockDbSet = MockDbSetHelper.CreateMockDbSet(mockLogs);
//             _mockContext.Setup(c => c.SecurityAuditLogs).Returns(mockDbSet.Object);
//
//             // Act
//             var count = await _service.GetFailedLoginCountAsync(ipAddress, since, CancellationToken.None);
//
//             // Assert
//             Assert.Equal(2, count);
//         }
//
//         [Fact]
//         public async Task CheckSuspiciousActivityAsync_MultipleFailedLogins_ReturnsTrue()
//         {
//             // Arrange
//             var ipAddress = "192.168.1.1";
//             var userId = Guid.NewGuid();
//
//             var mockLogs = new List<SecurityAuditLog>();
//             for (int i = 0; i < 6; i++) // 6 failed logins
//             {
//                 mockLogs.Add(new SecurityAuditLog
//                 {
//                     EventType = SecurityEventType.FailedLogin,
//                     IpAddress = ipAddress,
//                     Timestamp = DateTime.UtcNow.AddMinutes(-i) // All within last 5 minutes
//                 });
//             }
//
//             var mockDbSet = MockDbSetHelper.CreateMockDbSet(mockLogs);
//             _mockContext.Setup(c => c.SecurityAuditLogs).Returns(mockDbSet.Object);
//
//             var mockScopedContext = new Mock<IApplicationDbContext>();
//             var mockScopedSecurityLogs = new Mock<DbSet<SecurityAuditLog>>();
//             mockScopedContext.Setup(c => c.SecurityAuditLogs).Returns(mockScopedSecurityLogs.Object);
//             mockScopedContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
//
//             _mockServiceProvider.Setup(sp => sp.CreateScope())
//                 .Returns(() => {
//                     var scope = new Mock<IServiceScope>();
//                     scope.SetupGet(s => s.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
//                     scope.Setup(s => s.ServiceProvider.GetService(typeof(IApplicationDbContext)))
//                         .Returns(mockScopedContext.Object);
//                     return scope.Object;
//                 });
//
//             // Act
//             var isSuspicious = await _service.CheckSuspiciousActivityAsync(ipAddress, userId, CancellationToken.None);
//
//             // Assert
//             Assert.True(isSuspicious);
//             _mockLogger.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()),
//                 Times.Never);
//         }
//         // SmartPlanner.Tests/Application/Security/Services/AuditServiceTests.cs
// [Fact]
// public async Task LogSecurityEventAsync_ValidEvent_LogsEvent()
// {
//     // Arrange
//     var eventType = SecurityEventType.Login;
//     var userId = Guid.NewGuid();
//     var email = "test@example.com";
//     var ipAddress = "192.168.1.1";
//     var userAgent = "TestAgent";
//     var success = true;
//     var additionalData = new { Test = "Data" };
//
//     var mockSecurityLogs = new Mock<DbSet<SecurityAuditLog>>();
//     _mockContext.Setup(c => c.SecurityAuditLogs).Returns(mockSecurityLogs.Object);
//
//     var mockScopedContext = new Mock<IApplicationDbContext>();
//     var mockScopedSecurityLogs = new Mock<DbSet<SecurityAuditLog>>();
//     mockScopedContext.Setup(c => c.SecurityAuditLogs).Returns(mockScopedSecurityLogs.Object);
//     mockScopedContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
//
//     _mockServiceProvider.Setup(sp => sp.CreateScope())
//         .Returns(() => {
//             var scope = new Mock<IServiceScope>();
//             scope.SetupGet(s => s.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
//             scope.Setup(s => s.ServiceProvider.GetService(typeof(IApplicationDbContext)))
//                 .Returns(mockScopedContext.Object);
//             return scope.Object;
//         });
//
//     // Act
//     await _service.LogSecurityEventAsync(
//         eventType, userId, email, ipAddress, userAgent, success, additionalData, CancellationToken.None
//     );
//
//     // Assert
//     _mockLogger.Verify(l => l.LogDebug(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
// }
//
// // ДОПОЛНИТЕЛЬНЫЕ ТЕСТЫ
// [Fact]
// public async Task GetFailedLoginCountAsync_NoFailedLogins_ReturnsZero()
// {
//     // Arrange
//     var ipAddress = "192.168.1.1";
//     var since = DateTime.UtcNow.AddMinutes(-5);
//
//     var mockLogs = new List<SecurityAuditLog>
//     {
//         new SecurityAuditLog { EventType = SecurityEventType.Login, IpAddress = ipAddress, Timestamp = DateTime.UtcNow.AddMinutes(-1) }
//     };
//
//     var mockDbSet = MockDbSetHelper.CreateMockDbSet(mockLogs);
//     _mockContext.Setup(c => c.SecurityAuditLogs).Returns(mockDbSet.Object);
//
//     // Act
//     var count = await _service.GetFailedLoginCountAsync(ipAddress, since, CancellationToken.None);
//
//     // Assert
//     Assert.Equal(0, count);
// }
//
// [Fact]
// public async Task GetFailedLoginCountAsync_IpAddressNotFound_ReturnsZero()
// {
//     // Arrange
//     var ipAddress = "192.168.1.1";
//     var since = DateTime.UtcNow.AddMinutes(-5);
//
//     var mockLogs = new List<SecurityAuditLog>
//     {
//         new SecurityAuditLog { EventType = SecurityEventType.FailedLogin, IpAddress = "192.168.1.2", Timestamp = DateTime.UtcNow.AddMinutes(-1) }
//     };
//
//     var mockDbSet = MockDbSetHelper.CreateMockDbSet(mockLogs);
//     _mockContext.Setup(c => c.SecurityAuditLogs).Returns(mockDbSet.Object);
//
//     // Act
//     var count = await _service.GetFailedLoginCountAsync(ipAddress, since, CancellationToken.None);
//
//     // Assert
//     Assert.Equal(0, count);
// }
//
// [Fact]
// public async Task CheckSuspiciousActivityAsync_WithinThreshold_ReturnsFalse()
// {
//     // Arrange
//     var ipAddress = "192.168.1.1";
//     var userId = Guid.NewGuid();
//
//     var mockLogs = new List<SecurityAuditLog>
//     {
//         new SecurityAuditLog { EventType = SecurityEventType.FailedLogin, IpAddress = ipAddress, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
//         new SecurityAuditLog { EventType = SecurityEventType.FailedLogin, IpAddress = ipAddress, Timestamp = DateTime.UtcNow.AddMinutes(-2) }
//     };
//
//     var mockDbSet = MockDbSetHelper.CreateMockDbSet(mockLogs);
//     _mockContext.Setup(c => c.SecurityAuditLogs).Returns(mockDbSet.Object);
//
//     var mockScopedContext = new Mock<IApplicationDbContext>();
//     mockScopedContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
//
//     _mockServiceProvider.Setup(sp => sp.CreateScope())
//         .Returns(() => {
//             var scope = new Mock<IServiceScope>();
//             scope.SetupGet(s => s.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
//             scope.Setup(s => s.ServiceProvider.GetService(typeof(IApplicationDbContext)))
//                 .Returns(mockScopedContext.Object);
//             return scope.Object;
//         });
//
//     // Act
//     var isSuspicious = await _service.CheckSuspiciousActivityAsync(ipAddress, userId, CancellationToken.None);
//
//     // Assert
//     Assert.False(isSuspicious);
// }
//     }
// }
