// Tests/Unit/Application/Goals/Commands/Validators/GoalCommandValidatorsTests.cs
using FluentAssertions;
using FluentValidation.TestHelper;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Goals.Commands.Validators
{
    public class BulkCreateGoalsCommandValidatorTests
    {
        private readonly BulkCreateGoalsCommandValidator _validator;

        public BulkCreateGoalsCommandValidatorTests()
        {
            _validator = new BulkCreateGoalsCommandValidator();
        }

        [Fact]
        public void Should_BeValid_WhenAllFieldsCorrect()
        {
            // Arrange - исправлено создание CreateGoalDto
            var command = new BulkCreateGoalsCommand
            {
                UserId = Guid.NewGuid(),
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto(
                        Title: "Goal 1",
                        Description: "Description 1",
                        Category: "Health",
                        Priority: "High",
                        DueDate: DateTime.UtcNow.AddDays(30),
                        TargetValue: 100,
                        UserId: Guid.NewGuid() // Это UserId внутри DTO
                    ),
                    new CreateGoalDto(
                        Title: "Goal 2",
                        Description: "Description 2",
                        Category: "Education",
                        Priority: "Medium",
                        DueDate: DateTime.UtcNow.AddDays(60),
                        TargetValue: 50,
                        UserId: Guid.NewGuid()
                    )
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_HaveError_WhenUserIdEmpty()
        {
            // Arrange - исправлено создание CreateGoalDto
            var command = new BulkCreateGoalsCommand
            {
                UserId = Guid.Empty,
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto(
                        Title: "Goal 1",
                        Description: "Description 1",
                        Category: "Health",
                        Priority: "High",
                        DueDate: DateTime.UtcNow.AddDays(30),
                        TargetValue: 100,
                        UserId: Guid.NewGuid()
                    )
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID is required");
        }

        [Fact]
        public void Should_HaveError_WhenGoalsEmpty()
        {
            // Arrange
            var command = new BulkCreateGoalsCommand
            {
                UserId = Guid.NewGuid(),
                Goals = new List<CreateGoalDto>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Goals)
                .WithErrorMessage("At least one goal is required for bulk creation");
        }

        [Fact]
        public void Should_HaveError_WhenTooManyGoals()
        {
            // Arrange - исправлено создание CreateGoalDto
            var goals = Enumerable.Range(1, 101)
                .Select(i => new CreateGoalDto(
                    Title: $"Goal {i}",
                    Description: $"Description {i}",
                    Category: "Health",
                    Priority: "Medium",
                    DueDate: DateTime.UtcNow.AddDays(30),
                    TargetValue: 100,
                    UserId: Guid.NewGuid()
                ))
                .ToList();

            var command = new BulkCreateGoalsCommand
            {
                UserId = Guid.NewGuid(),
                Goals = goals
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Goals)
                .WithErrorMessage("Cannot create more than 100 goals at once");
        }

        [Fact]
        public void Should_ValidateIndividualGoals()
        {
            // Arrange - исправлено создание CreateGoalDto
            var command = new BulkCreateGoalsCommand
            {
                UserId = Guid.NewGuid(),
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto(
                        Title: "", // Ошибка: пустое название
                        Description: "Description 1",
                        Category: "Health",
                        Priority: "Medium",
                        DueDate: DateTime.UtcNow.AddDays(30),
                        TargetValue: 100,
                        UserId: Guid.NewGuid()
                    ),
                    new CreateGoalDto(
                        Title: "Valid Goal",
                        Description: "Description 2",
                        Category: "Health",
                        Priority: "Medium",
                        DueDate: DateTime.UtcNow.AddDays(-1), // Ошибка: дата в прошлом
                        TargetValue: 100,
                        UserId: Guid.NewGuid()
                    )
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Goals[0].Title")
                .WithErrorMessage("Goal title is required");
            result.ShouldHaveValidationErrorFor("Goals[1].DueDate")
                .WithErrorMessage("Due date must be in the future");
        }

        [Fact]
        public void Should_HaveError_WhenGoalHasZeroTargetValue()
        {
            // Arrange - исправлено создание CreateGoalDto
            var command = new BulkCreateGoalsCommand
            {
                UserId = Guid.NewGuid(),
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto(
                        Title: "Goal 1",
                        Description: "Description 1",
                        Category: "Health",
                        Priority: "Medium",
                        DueDate: DateTime.UtcNow.AddDays(30),
                        TargetValue: 0, // Ошибка: нулевое значение
                        UserId: Guid.NewGuid()
                    )
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Goals[0].TargetValue")
                .WithErrorMessage("Target value must be positive");
        }

        [Fact]
        public void Should_HaveError_WhenGoalHasNegativeTargetValue()
        {
            // Arrange - исправлено создание CreateGoalDto
            var command = new BulkCreateGoalsCommand
            {
                UserId = Guid.NewGuid(),
                Goals = new List<CreateGoalDto>
                {
                    new CreateGoalDto(
                        Title: "Goal 1",
                        Description: "Description 1",
                        Category: "Health",
                        Priority: "Medium",
                        DueDate: DateTime.UtcNow.AddDays(30),
                        TargetValue: -10, // Ошибка: отрицательное значение
                        UserId: Guid.NewGuid()
                    )
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Goals[0].TargetValue")
                .WithErrorMessage("Target value must be positive");
        }
    }

    public class BulkUpdateGoalsCommandValidatorTests
    {
        private readonly BulkUpdateGoalsCommandValidator _validator;

        public BulkUpdateGoalsCommandValidatorTests()
        {
            _validator = new BulkUpdateGoalsCommandValidator();
        }

        [Fact]
        public void Should_BeValid_WhenAllFieldsCorrect()
        {
            // Arrange - исправлено создание UpdateGoalDto
            var command = new BulkUpdateGoalsCommand
            {
                Goals = new List<BulkUpdateGoalItem>
                {
                    new BulkUpdateGoalItem
                    {
                        GoalId = Guid.NewGuid(),
                        UpdateData = new UpdateGoalDto(
                            Title: "Updated Title",
                            Description: "Updated Description",
                            Category: "Health",
                            Priority: "High",
                            DueDate: DateTime.UtcNow.AddDays(60),
                            TargetValue: 200
                        )
                    }
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_HaveError_WhenGoalsEmpty()
        {
            // Arrange
            var command = new BulkUpdateGoalsCommand
            {
                Goals = new List<BulkUpdateGoalItem>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Goals)
                .WithErrorMessage("At least one goal update is required");
        }

        [Fact]
        public void Should_HaveError_WhenTooManyGoals()
        {
            // Arrange - исправлено создание UpdateGoalDto
            var goals = Enumerable.Range(1, 51)
                .Select(i => new BulkUpdateGoalItem
                {
                    GoalId = Guid.NewGuid(),
                    UpdateData = new UpdateGoalDto(
                        Title: $"Updated Title {i}",
                        Description: $"Updated Description {i}",
                        Category: "Health",
                        Priority: "Medium",
                        DueDate: DateTime.UtcNow.AddDays(30),
                        TargetValue: 100
                    )
                })
                .ToList();

            var command = new BulkUpdateGoalsCommand
            {
                Goals = goals
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Goals)
                .WithErrorMessage("Cannot update more than 50 goals at once");
        }

        [Fact]
        public void Should_HaveError_WhenGoalIdEmpty()
        {
            // Arrange - исправлено создание UpdateGoalDto
            var command = new BulkUpdateGoalsCommand
            {
                Goals = new List<BulkUpdateGoalItem>
                {
                    new BulkUpdateGoalItem
                    {
                        GoalId = Guid.Empty, // Ошибка: пустой Guid
                        UpdateData = new UpdateGoalDto(
                            Title: "Updated Title",
                            Description: "Updated Description",
                            Category: "Health",
                            Priority: "Medium",
                            DueDate: DateTime.UtcNow.AddDays(30),
                            TargetValue: 100
                        )
                    }
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Goals[0].GoalId")
                .WithErrorMessage("Goal ID is required");
        }

        [Fact]
        public void Should_HaveError_WhenUpdateDataIsNull()
        {
            // Arrange
            var command = new BulkUpdateGoalsCommand
            {
                Goals = new List<BulkUpdateGoalItem>
                {
                    new BulkUpdateGoalItem
                    {
                        GoalId = Guid.NewGuid(),
                        UpdateData = null! // Ошибка: null UpdateData
                    }
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Goals[0].UpdateData")
                .WithErrorMessage("Update data is required");
        }

        [Fact]
        public void Should_AllowPartialUpdateData()
        {
            // Arrange - UpdateGoalDto может иметь null значения для частичного обновления
            var command = new BulkUpdateGoalsCommand
            {
                Goals = new List<BulkUpdateGoalItem>
                {
                    new BulkUpdateGoalItem
                    {
                        GoalId = Guid.NewGuid(),
                        UpdateData = new UpdateGoalDto(
                            Title: "Only Title Updated",
                            Description: null, // Разрешено null
                            Category: null,
                            Priority: null,
                            DueDate: null,
                            TargetValue: null
                        )
                    }
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    public class BulkDeleteGoalsCommandValidatorTests
    {
        private readonly BulkDeleteGoalsCommandValidator _validator;

        public BulkDeleteGoalsCommandValidatorTests()
        {
            _validator = new BulkDeleteGoalsCommandValidator();
        }

        [Fact]
        public void Should_HaveError_WhenGoalIdsEmpty()
        {
            // Arrange
            var command = new BulkDeleteGoalsCommand
            {
                GoalIds = new List<Guid>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.GoalIds)
                .WithErrorMessage("At least one goal ID is required for bulk deletion");
        }

        [Fact]
        public void Should_HaveError_WhenTooManyGoalIds()
        {
            // Arrange
            var goalIds = Enumerable.Range(1, 101)
                .Select(_ => Guid.NewGuid())
                .ToList();

            var command = new BulkDeleteGoalsCommand
            {
                GoalIds = goalIds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.GoalIds)
                .WithErrorMessage("Cannot delete more than 100 goals at once");
        }

        [Fact]
        public void Should_HaveError_WhenContainsEmptyGuid()
        {
            // Arrange
            var command = new BulkDeleteGoalsCommand
            {
                GoalIds = new List<Guid> { Guid.NewGuid(), Guid.Empty, Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.GoalIds)
                .WithErrorMessage("All goal IDs must be valid");
        }

        [Fact]
        public void Should_BeValid_WhenAllIdsValid()
        {
            // Arrange
            var command = new BulkDeleteGoalsCommand
            {
                GoalIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    public class CreateGoalCommandValidatorTests
    {
        private readonly CreateGoalCommandValidator _validator;

        public CreateGoalCommandValidatorTests()
        {
            _validator = new CreateGoalCommandValidator();
        }

        [Fact]
        public void Should_HaveError_WhenTitleEmpty()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = string.Empty,
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Goal title is required");
        }

        [Fact]
        public void Should_HaveError_WhenTitleTooLong()
        {
            // Arrange
            var longTitle = new string('A', 501);
            var command = new CreateGoalCommand
            {
                Title = longTitle,
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Goal title cannot exceed 500 characters");
        }

        [Fact]
        public void Should_HaveError_WhenDescriptionTooLong()
        {
            // Arrange
            var longDescription = new string('A', 2001);
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Description = longDescription,
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description cannot exceed 2000 characters");
        }

        [Fact]
        public void Should_HaveError_WhenCategoryEmpty()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = string.Empty,
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Category)
                .WithErrorMessage("Category is required");
        }

        [Fact]
        public void Should_HaveError_WhenCategoryInvalid()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = "InvalidCategory",
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Category)
                .WithErrorMessage("Invalid goal category");
        }

        [Fact]
        public void Should_HaveError_WhenPriorityInvalid()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = GoalCategory.Health.ToString(),
                Priority = "InvalidPriority",
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Priority)
                .WithErrorMessage("Invalid goal priority");
        }

        [Fact]
        public void Should_HaveError_WhenDueDateInPast()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(-1),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DueDate)
                .WithErrorMessage("Due date must be in the future");
        }

        [Fact]
        public void Should_HaveError_WhenTargetValueZero()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 0,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetValue)
                .WithErrorMessage("Target value must be positive");
        }

        [Fact]
        public void Should_HaveError_WhenUserIdEmpty()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = GoalCategory.Health.ToString(),
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID is required");
        }

        [Fact]
        public void Should_BeValid_WhenAllFieldsCorrect()
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Description = "Valid Description",
                Category = GoalCategory.Education.ToString(),
                Priority = GoalPriority.High.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("Sports")]
        [InlineData("Education")]
        [InlineData("Finance")]
        [InlineData("Hobbies")]
        public void Should_AcceptAllValidCategories(string category)
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = category,
                Priority = GoalPriority.Medium.ToString(),
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Category);
        }

        [Theory]
        [InlineData("Low")]
        [InlineData("Medium")]
        [InlineData("High")]
        [InlineData("Critical")]
        public void Should_AcceptAllValidPriorities(string priority)
        {
            // Arrange
            var command = new CreateGoalCommand
            {
                Title = "Valid Title",
                Category = GoalCategory.Health.ToString(),
                Priority = priority,
                DueDate = DateTime.UtcNow.AddDays(30),
                TargetValue = 100,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Priority);
        }
    }
}
