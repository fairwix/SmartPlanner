// SmartPlanner.Tests/Application/Authorization/Requirements/ResourceOwnerRequirementTests.cs
using SmartPlanner.Application.Authorization.Requirements;
using Xunit;

namespace SmartPlanner.Tests.Application.Authorization.Requirements
{
    public class ResourceOwnerRequirementTests
    {
        [Fact]
        public void Constructor_CreatesInstance()
        {
            // Act
            var requirement = new ResourceOwnerRequirement();

            // Assert
            Assert.NotNull(requirement);
        }
    }
}
