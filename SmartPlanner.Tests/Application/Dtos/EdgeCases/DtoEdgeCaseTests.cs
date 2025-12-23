// Tests/Unit/Application/DTOs/EdgeCases/DtoEdgeCaseTests.cs
using FluentAssertions;
using SmartPlanner.Application.DTOs.Challenge;
using SmartPlanner.Application.DTOs.Goal;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.DTOs.EdgeCases
{
    public class DtoEdgeCaseTests
    {
        [Fact]
        public void CreateChallengeRequest_ShouldHandleMinimumValues()
        {
            // Arrange
            var request = new CreateChallengeRequest(
                "A", // Minimum title length? (validator allows 1 char)
                "",  // Empty description
                ChallengeType.Custom,
                DateTime.UtcNow.AddTicks(1), // Just barely in future
                DateTime.UtcNow.AddTicks(2), // Just after start
                false,
                1,   // Minimum target value
                Guid.Empty // Empty guid
            );

            // Assert
            request.Should().NotBeNull();
            request.TargetValue.Should().Be(1);
            request.CreatedBy.Should().Be(Guid.Empty);
        }

        [Fact]
        public void CreateGoalRequest_ShouldHandleMaximumValues()
        {
            // Arrange
            var maxTitle = new string('A', 500); // Max title length from validator
            var request = new CreateGoalRequest(
                maxTitle,
                new string('B', 2000), // Long description
                GoalCategory.Hobbies,
                GoalPriority.Critical,
                DateTime.MaxValue, // Far future
                int.MaxValue,      // Max target value
                Guid.NewGuid()
            );

            // Assert
            request.Should().NotBeNull();
            request.TargetValue.Should().Be(int.MaxValue);
        }

        [Fact]
        public void ChallengeResponse_ShouldHandleBoundaryProgress()
        {
            // Test 0% progress
            var response1 = new ChallengeResponse(
                Guid.NewGuid(),
                "Test",
                "Test",
                ChallengeType.StepCount,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(1),
                false,
                100,
                0,
                0.0,
                false,
                Guid.NewGuid(),
                DateTime.UtcNow
            );
            response1.GroupProgressPercentage.Should().Be(0.0);

            // Test 100% progress
            var response2 = new ChallengeResponse(
                Guid.NewGuid(),
                "Test",
                "Test",
                ChallengeType.StepCount,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(1),
                false,
                100,
                100,
                100.0,
                false,
                Guid.NewGuid(),
                DateTime.UtcNow
            );
            response2.GroupProgressPercentage.Should().Be(100.0);
        }

        [Fact]
        public void GoalResponse_ShouldHandleBooleanCombinations()
        {
            // Test all possible boolean combinations
            var combinations = new[]
            {
                (IsCompleted: false, IsAiGenerated: false, IsExpired: false, IsOnTrack: false),
                (IsCompleted: true, IsAiGenerated: false, IsExpired: false, IsOnTrack: false),
                (IsCompleted: false, IsAiGenerated: true, IsExpired: false, IsOnTrack: false),
                (IsCompleted: false, IsAiGenerated: false, IsExpired: true, IsOnTrack: false),
                (IsCompleted: false, IsAiGenerated: false, IsExpired: false, IsOnTrack: true),
                (IsCompleted: true, IsAiGenerated: true, IsExpired: true, IsOnTrack: true)
            };

            foreach (var (isCompleted, isAiGenerated, isExpired, isOnTrack) in combinations)
            {
                var response = new GoalResponse(
                    Guid.NewGuid(),
                    "Test Goal",
                    "Test Description",
                    GoalCategory.Personal,
                    GoalPriority.Medium,
                    DateTime.UtcNow.AddDays(30),
                    100,
                    50,
                    50.0,
                    isCompleted,
                    isAiGenerated,
                    100,
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    isExpired,
                    isOnTrack
                );

                response.IsCompleted.Should().Be(isCompleted);
                response.IsAiGenerated.Should().Be(isAiGenerated);
                response.IsExpired.Should().Be(isExpired);
                response.IsOnTrack.Should().Be(isOnTrack);
            }
        }
    }
}
