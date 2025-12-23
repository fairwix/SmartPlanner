// SmartPlanner.Tests/Application/Authorization/Handlers/ResourceOwnerAuthorizationHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPlanner.Application.Authorization.Handlers;
using SmartPlanner.Application.Authorization.Requirements;
using Xunit;

namespace SmartPlanner.Tests.Application.Authorization.Handlers
{
    public class ResourceOwnerAuthorizationHandlerTests
    {
        private readonly ResourceOwnerAuthorizationHandler _handler;

        public ResourceOwnerAuthorizationHandlerTests()
        {
            _handler = new ResourceOwnerAuthorizationHandler();
        }

        [Fact]
        public async Task HandleRequirementAsync_NoUserIdClaim_DoesNotSucceed()
        {
            // Arrange
            var resourceUserId = Guid.NewGuid();
            var resource = new TestResource { UserId = resourceUserId }; // Реализует IUserOwnedResource

            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Name, "testuser") })), // Нет userId
                resource
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_UserIsOwner_Succeeds()
        {
            // Arrange
            var resourceUserId = Guid.NewGuid();
            var resource = new TestResource { UserId = resourceUserId }; // Реализует IUserOwnedResource

            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("userId", resourceUserId.ToString()) })), // userId совпадает
                resource
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_UserIsNotOwner_DoesNotSucceed()
        {
            // Arrange
            var resourceUserId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var resource = new TestResource { UserId = resourceUserId }; // Реализует IUserOwnedResource

            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("userId", differentUserId.ToString()) })), // userId НЕ совпадает
                resource
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

         [Fact]
        public async Task HandleRequirementAsync_ResourceIsNotIUserOwnedResource_DoesNotSucceed()
        {
             // Arrange
            var nonUserOwnedResource = new object(); // Не реализует IUserOwnedResource

            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("userId", Guid.NewGuid().ToString()) })),
                nonUserOwnedResource // Передаём ресурс, не реализующий интерфейс
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

         [Fact]
        public async Task HandleRequirementAsync_UserIdClaimIsEmpty_DoesNotSucceed()
        {
             // Arrange
            var resourceUserId = Guid.NewGuid();
            var resource = new TestResource { UserId = resourceUserId };

            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("userId", "") })), // Пустой userId
                resource
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

         [Fact]
        public async Task HandleRequirementAsync_UserIdClaimIsNull_DoesNotSucceed()
        {
             // Arrange
            var resourceUserId = Guid.NewGuid();
            var resource = new TestResource { UserId = resourceUserId };

            var context = new AuthorizationHandlerContext(
                new[] { new ResourceOwnerRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("userId", null!) })), // Null userId (если возможно)
                resource
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }
    }

    // Вспомогательный класс для тестирования, реализующий IUserOwnedResource
    public class TestResource : IUserOwnedResource
    {
        public Guid UserId { get; set; }
    }
}
