// SmartPlanner.Tests/Application/Common/Mapping/MappingProfileTests.cs
using Xunit;
using AutoMapper;
using SmartPlanner.Application.Common.Mapping;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Challenges.Dtos;
using System;
using System.Collections.Generic;

namespace SmartPlanner.Tests.Application.Common.Mapping
{
    public class MappingProfileTests
    {
        private readonly IMapper _mapper;

        public MappingProfileTests()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = configuration.CreateMapper();
        }

        [Fact]
        public void ShouldMapGoalToGoalDto()
        {
            // Arrange
            var goal = new Goal
            {
                Id = Guid.NewGuid(),
                Title = "Test Goal",
                Description = "Test Description",
                Category = GoalCategory.Sports,
                Priority = GoalPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(7),
                TargetValue = 100,
                CurrentValue = 50,
                IsCompleted = false,
                IsAiGenerated = false,
                RewardAmount = 10,
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var goalDto = _mapper.Map<GoalDto>(goal);

            // Assert
            Assert.Equal(goal.Title, goalDto.Title);
            Assert.Equal(goal.Description, goalDto.Description);
            Assert.Equal("Sports", goalDto.Category);
            Assert.Equal("Medium", goalDto.Priority);
        }

        [Fact]
        public void ShouldMapUserToUserDto()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                Balance = 100,
                StreakCount = 5,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserInterests = new List<UserInterest>
                {
                    new UserInterest { Interest = new Interest { Name = "Programming" } },
                    new UserInterest { Interest = new Interest { Name = "Sports" } }
                }
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            Assert.Equal(user.Username, userDto.Username);
            Assert.Equal(user.Email, userDto.Email);
            Assert.Equal(user.Balance, userDto.Balance);
            Assert.Equal(2, userDto.Interests.Count);
        }

        [Fact]
        public void ShouldMapAchievementToAchievementDto()
        {
            // Arrange
            var achievement = new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Test Achievement",
                Description = "Test Description",
                BadgeImage = "/badge.png",
                RewardAmount = 100,
                Type = AchievementType.Streak,
                Condition = "streak:7",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var achievementDto = _mapper.Map<AchievementDto>(achievement);

            // Assert
            Assert.Equal(achievement.Name, achievementDto.Name);
            Assert.Equal(achievement.RewardAmount, achievementDto.RewardAmount);
            Assert.Equal("Streak", achievementDto.Type);
        }
    }
}
