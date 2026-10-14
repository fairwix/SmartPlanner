// // SmartPlanner.Tests/Application/Auth/Validators/LoginCommandValidatorTests.cs
// using FluentValidation.TestHelper;
// using SmartPlanner.Application.Auth.Commands;
// using SmartPlanner.Application.Auth.Validators;
// using Xunit;
//
// namespace SmartPlanner.Tests.Application.Auth.Validators
// {
//     public class LoginCommandValidatorTests
//     {
//         private readonly LoginCommandValidator _validator;
//
//         public LoginCommandValidatorTests()
//         {
//             _validator = new LoginCommandValidator();
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_EmailOrUsername_Is_Empty()
//         {
//             // Arrange
//             var command = new LoginCommand { EmailOrUsername = "", Password = "SomePassword" };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.EmailOrUsername);
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_Password_Is_Empty()
//         {
//             // Arrange
//             var command = new LoginCommand { EmailOrUsername = "user", Password = "" };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.Password);
//         }
//
//         [Fact]
//         public void Should_Not_Have_Error_When_Command_Is_Valid()
//         {
//             // Arrange
//             var command = new LoginCommand { EmailOrUsername = "user@example.com", Password = "ValidPass123!" };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldNotHaveAnyValidationErrors();
//         }
//     }
// }
