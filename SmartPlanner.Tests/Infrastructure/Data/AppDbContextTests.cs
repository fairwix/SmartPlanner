using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Data;
using Xunit;

namespace SmartPlanner.Tests.Infrastructure.Data
{
    public class AppDbContextTests : IDisposable
    {
        private readonly AppDbContext _context;

        public AppDbContextTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldSetUpdatedAtOnModifiedEntities()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var initialUpdatedAt = user.UpdatedAt;

            // Act
            user.Username = "updateduser";
            await Task.Delay(100); // Ensure time difference
            await _context.SaveChangesAsync();

            // Assert
            user.UpdatedAt.Should().BeAfter(initialUpdatedAt);
        }

        [Fact]
        public async Task User_CanHaveMultipleGoals()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash"
            };

            var goals = new List<Goal>
            {
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Goal 1",
                    UserId = user.Id,
                    TargetValue = 100
                },
                new Goal
                {
                    Id = Guid.NewGuid(),
                    Title = "Goal 2",
                    UserId = user.Id,
                    TargetValue = 200
                }
            };

            // Act
            _context.Users.Add(user);
            _context.Goals.AddRange(goals);
            await _context.SaveChangesAsync();

            // Assert
            var savedUser = await _context.Users
                .Include(u => u.Goals)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            savedUser.Should().NotBeNull();
            savedUser!.Goals.Should().HaveCount(2);
        }

        [Fact]
        public async Task Goal_CanHaveProgressHistory()
        {
            // Arrange
            var goal = new Goal
            {
                Id = Guid.NewGuid(),
                Title = "Test Goal",
                UserId = Guid.NewGuid(),
                TargetValue = 100
            };

            var progress = new GoalProgress
            {
                Id = Guid.NewGuid(),
                GoalId = goal.Id,
                Value = 50,
                PreviousValue = 30
            };

            // Act
            _context.Goals.Add(goal);
            _context.GoalProgresses.Add(progress);
            await _context.SaveChangesAsync();

            // Assert
            var savedGoal = await _context.Goals
                .Include(g => g.ProgressHistory)
                .FirstOrDefaultAsync(g => g.Id == goal.Id);

            savedGoal.Should().NotBeNull();
            savedGoal!.ProgressHistory.Should().HaveCount(1);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
