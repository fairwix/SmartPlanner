// // SmartPlanner.Tests/Application/Auth/Validators/RegisterCommandValidatorTests.cs
// using System;
// using System.Threading.Tasks;
// using FluentValidation.TestHelper;
// using SmartPlanner.Application.Auth.Commands;
// using SmartPlanner.Application.Auth.Validators;
// using Xunit;
//
// namespace SmartPlanner.Tests.Application.Auth.Validators
// {
//     public class RegisterCommandValidatorTests
//     {
//         private readonly RegisterCommandValidator _validator;
//
//         public RegisterCommandValidatorTests()
//         {
//             _validator = new RegisterCommandValidator();
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_Email_Is_Empty()
//         {
//             // Arrange
//             var command = new RegisterCommand { Email = "", Username = "testuser", Password = "ValidPass123!", DateOfBirth = DateTime.UtcNow.AddYears(-25) };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.Email);
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_Email_Is_Invalid()
//         {
//             // Arrange
//             var command = new RegisterCommand { Email = "not-an-email", Username = "testuser", Password = "ValidPass123!", DateOfBirth = DateTime.UtcNow.AddYears(-25) };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.Email);
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_Username_Is_Null()
//         {
//             // Arrange
//             var command = new RegisterCommand { Email = "test@example.com", Username = null!, Password = "ValidPass123!", DateOfBirth = DateTime.UtcNow.AddYears(-25) };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.Username);
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_Username_Is_Too_Short()
//         {
//             // Arrange
//             var command = new RegisterCommand { Email = "test@example.com", Username = "usr", Password = "ValidPass123!", DateOfBirth = DateTime.UtcNow.AddYears(-25) };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.Username);
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_Password_Does_Not_Match_Requirements()
//         {
//             // Arrange
//             var command = new RegisterCommand { Email = "test@example.com", Username = "testuser", Password = "weak", DateOfBirth = DateTime.UtcNow.AddYears(-25) };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.Password);
//         }
//
//         [Fact]
//         public void Should_Have_Error_When_DateOfBirth_Indicates_Minor()
//         {
//             // Arrange
//             var command = new RegisterCommand { Email = "test@example.com", Username = "testuser", Password = "ValidPass123!", DateOfBirth = DateTime.UtcNow.AddYears(-15) };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
//         }
//
//         [Fact]
//         public void Should_Not_Have_Error_When_Command_Is_Valid()
//         {
//             // Arrange
//             var command = new RegisterCommand { Email = "valid@example.com", Username = "validuser", Password = "ValidPass123!", DateOfBirth = DateTime.UtcNow.AddYears(-25) };
//
//             // Act
//             var result = _validator.TestValidate(command);
//
//             // Assert
//             result.ShouldNotHaveAnyValidationErrors();
//         }
//     }
// }
