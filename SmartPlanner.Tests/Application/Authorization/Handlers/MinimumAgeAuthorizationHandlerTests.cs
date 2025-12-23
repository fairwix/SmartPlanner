// SmartPlanner.Tests/Application/Authorization/Handlers/MinimumAgeAuthorizationHandlerTests.cs
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
    public class MinimumAgeAuthorizationHandlerTests
    {
        private readonly MinimumAgeAuthorizationHandler _handler;

        public MinimumAgeAuthorizationHandlerTests()
        {
            _handler = new MinimumAgeAuthorizationHandler();
        }

        [Fact]
        public async Task HandleRequirementAsync_NoDateOfBirthClaim_DoesNotSucceed()
        {
            // Arrange
            var context = new AuthorizationHandlerContext(
                new[] { new MinimumAgeRequirement(18) },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Name, "testuser") })), // Нет dateOfBirth
                resource: null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_InvalidDateOfBirthClaim_DoesNotSucceed()
        {
            // Arrange
            var context = new AuthorizationHandlerContext(
                new[] { new MinimumAgeRequirement(18) },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("dateOfBirth", "not_a_date") })),
                resource: null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_UserIsOlderThanMinimum_Succeeds()
        {
            // Arrange
            var dob = DateTime.Today.AddYears(-25); // 25 лет
            var context = new AuthorizationHandlerContext(
                new[] { new MinimumAgeRequirement(18) },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("dateOfBirth", dob.ToString("yyyy-MM-dd")) })),
                resource: null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_UserIsYoungerThanMinimum_DoesNotSucceed()
        {
            // Arrange
            var dob = DateTime.Today.AddYears(-15); // 15 лет
            var context = new AuthorizationHandlerContext(
                new[] { new MinimumAgeRequirement(18) },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("dateOfBirth", dob.ToString("yyyy-MM-dd")) })),
                resource: null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_UserIsExactlyMinimumAge_Succeeds()
        {
            // Arrange
            var today = DateTime.Today;
            var dob = today.AddYears(-18); // Ровно 18 лет, если сегодня день рождения
            var context = new AuthorizationHandlerContext(
                new[] { new MinimumAgeRequirement(18) },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("dateOfBirth", dob.ToString("yyyy-MM-dd")) })),
                resource: null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

         [Fact]
        public async Task HandleRequirementAsync_UserIsOneDayBeforeMinimumAge_DoesNotSucceed()
        {
             // Arrange
            var today = DateTime.Today;
            var dob = today.AddYears(-18).AddDays(1); // 17 лет и 364 дня
            var context = new AuthorizationHandlerContext(
                new[] { new MinimumAgeRequirement(18) },
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("dateOfBirth", dob.ToString("yyyy-MM-dd")) })),
                resource: null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }
    }
}
