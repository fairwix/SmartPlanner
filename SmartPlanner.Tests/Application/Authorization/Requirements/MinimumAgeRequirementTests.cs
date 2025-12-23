// SmartPlanner.Tests/Application/Authorization/Requirements/MinimumAgeRequirementTests.cs
using SmartPlanner.Application.Authorization.Requirements;
using Xunit;

namespace SmartPlanner.Tests.Application.Authorization.Requirements
{
    public class MinimumAgeRequirementTests
    {
        [Fact]
        public void Constructor_SetsMinimumAge()
        {
            // Arrange
            var minimumAge = 21;

            // Act
            var requirement = new MinimumAgeRequirement(minimumAge);

            // Assert
            Assert.Equal(minimumAge, requirement.MinimumAge);
        }
    }
}
