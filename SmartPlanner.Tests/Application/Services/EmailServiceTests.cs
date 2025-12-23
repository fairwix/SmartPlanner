// Tests/Application/Services/EmailServiceTests.cs

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartPlanner.Application.Common.Models;
using SmartPlanner.Application.Services;
using Xunit;

namespace SmartPlanner.Application.UnitTests.Services;

public class EmailServiceTests
{
    private readonly Mock<IOptions<EmailSettings>> _mockEmailSettings;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly EmailService _service;
    private readonly EmailSettings _emailSettings;

    public EmailServiceTests()
    {
        _emailSettings = new EmailSettings
        {
            SmtpServer = "smtp.example.com",
            SmtpPort = 587,
            SenderEmail = "noreply@smartplanner.com",
            SenderName = "Smart Planner",
            UseSsl = true,
            RequiresAuthentication = false,
            UseFileSystem = false
        };

        _mockEmailSettings = new Mock<IOptions<EmailSettings>>();
        _mockEmailSettings.Setup(x => x.Value).Returns(_emailSettings);

        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        _mockConfiguration = new Mock<IConfiguration>();

        _service = new EmailService(
            _mockEmailSettings.Object,
            _mockLogger.Object,
            _mockEnvironment.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task SendEmailAsync_DevelopmentEnvironmentWithoutFileSystem_LogsEmail()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _emailSettings.UseFileSystem = false;

        var to = "test@example.com";
        var subject = "Test Subject";
        var body = "<p>Test Body</p>";

        // Act
        await _service.SendEmailAsync(to, subject, body);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[DEV EMAIL]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailConfirmationAsync_CallsSendEmailAsyncWithCorrectParameters()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _mockConfiguration.Setup(c => c["App:BaseUrl"]).Returns("https://localhost:5001");

        var email = "user@example.com";
        var userName = "Test User";
        var confirmationLink = "https://localhost:5001/confirm-email?token=abc";

        // Capture the actual call to SendEmailAsync
        var capturedTo = "";
        var capturedSubject = "";
        var capturedBody = "";

        var mockService = new Mock<EmailService>(
            _mockEmailSettings.Object,
            _mockLogger.Object,
            _mockEnvironment.Object,
            _mockConfiguration.Object)
        {
            CallBase = true
        };

        mockService.Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .Callback<string, string, string, bool>((to, subject, body, isHtml) =>
            {
                capturedTo = to;
                capturedSubject = subject;
                capturedBody = body;
            })
            .Returns(Task.CompletedTask);

        // Act
        await mockService.Object.SendEmailConfirmationAsync(email, userName, confirmationLink);

        // Assert
        capturedTo.Should().Be(email);
        capturedSubject.Should().Be("Confirm your email - Smart Planner");
        capturedBody.Should().Contain(userName);
        capturedBody.Should().Contain(confirmationLink);
        capturedBody.Should().Contain("Confirm Email Address");
    }

    [Fact]
    public async Task SendPasswordResetAsync_CallsSendEmailAsyncWithCorrectParameters()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _mockConfiguration.Setup(c => c["App:BaseUrl"]).Returns("https://localhost:5001");

        var email = "user@example.com";
        var userName = "Test User";
        var resetLink = "https://localhost:5001/reset-password?token=xyz";

        // Capture the actual call to SendEmailAsync
        var capturedTo = "";
        var capturedSubject = "";
        var capturedBody = "";

        var mockService = new Mock<EmailService>(
            _mockEmailSettings.Object,
            _mockLogger.Object,
            _mockEnvironment.Object,
            _mockConfiguration.Object)
        {
            CallBase = true
        };

        mockService.Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .Callback<string, string, string, bool>((to, subject, body, isHtml) =>
            {
                capturedTo = to;
                capturedSubject = subject;
                capturedBody = body;
            })
            .Returns(Task.CompletedTask);

        // Act
        await mockService.Object.SendPasswordResetAsync(email, userName, resetLink);

        // Assert
        capturedTo.Should().Be(email);
        capturedSubject.Should().Be("Reset your password - Smart Planner");
        capturedBody.Should().Contain(userName);
        capturedBody.Should().Contain(resetLink);
        capturedBody.Should().Contain("Reset Password");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_CallsSendEmailAsyncWithCorrectParameters()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _mockConfiguration.Setup(c => c["App:BaseUrl"]).Returns("https://localhost:5001");

        var email = "user@example.com";
        var userName = "Test User";

        // Capture the actual call to SendEmailAsync
        var capturedTo = "";
        var capturedSubject = "";
        var capturedBody = "";

        var mockService = new Mock<EmailService>(
            _mockEmailSettings.Object,
            _mockLogger.Object,
            _mockEnvironment.Object,
            _mockConfiguration.Object)
        {
            CallBase = true
        };

        mockService.Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .Callback<string, string, string, bool>((to, subject, body, isHtml) =>
            {
                capturedTo = to;
                capturedSubject = subject;
                capturedBody = body;
            })
            .Returns(Task.CompletedTask);

        // Act
        await mockService.Object.SendWelcomeEmailAsync(email, userName);

        // Assert
        capturedTo.Should().Be(email);
        capturedSubject.Should().Be("🎉 Welcome to Smart Planner!");
        capturedBody.Should().Contain(userName);
        capturedBody.Should().Contain("Welcome aboard");
        capturedBody.Should().Contain("Create Your First Goal");
    }

    [Fact]
    public async Task SendAccountLockedNotificationAsync_CallsSendEmailAsyncWithCorrectParameters()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);

        var email = "user@example.com";
        var userName = "Test User";
        var lockoutEnd = DateTime.UtcNow.AddHours(1);

        // Capture the actual call to SendEmailAsync
        var capturedTo = "";
        var capturedSubject = "";
        var capturedBody = "";

        var mockService = new Mock<EmailService>(
            _mockEmailSettings.Object,
            _mockLogger.Object,
            _mockEnvironment.Object,
            _mockConfiguration.Object)
        {
            CallBase = true
        };

        mockService.Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .Callback<string, string, string, bool>((to, subject, body, isHtml) =>
            {
                capturedTo = to;
                capturedSubject = subject;
                capturedBody = body;
            })
            .Returns(Task.CompletedTask);

        // Act
        await mockService.Object.SendAccountLockedNotificationAsync(email, userName, lockoutEnd);

        // Assert
        capturedTo.Should().Be(email);
        capturedSubject.Should().Be("⚠️ Account Locked - Smart Planner");
        capturedBody.Should().Contain(userName);
        capturedBody.Should().Contain("Account Security Alert");
        capturedBody.Should().Contain(lockoutEnd.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}
