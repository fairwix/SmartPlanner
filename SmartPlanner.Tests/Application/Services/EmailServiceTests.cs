using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartPlanner.Application.Common.Models;
using SmartPlanner.Application.Services;
using Xunit;

namespace SmartPlanner.Application.Tests.Services
{
    public class EmailServiceTests : IDisposable
    {
        private readonly Mock<IOptions<EmailSettings>> _mockEmailSettings;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly Mock<IHostEnvironment> _mockEnvironment;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly EmailService _service;
        private readonly string _testEmailsDirectory;

        public EmailServiceTests()
        {
            _mockEmailSettings = new Mock<IOptions<EmailSettings>>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockEnvironment = new Mock<IHostEnvironment>();
            _mockConfiguration = new Mock<IConfiguration>();

            var emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.test.com",
                SmtpPort = 587,
                UseSsl = true,
                SenderEmail = "test@smartplanner.com",
                SenderName = "Smart Planner Test",
                RequiresAuthentication = true,
                UseFileSystem = false,
                FileSystemPath = "test_emails"
            };

            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            // Создаем временную директорию для тестов
            _testEmailsDirectory = Path.Combine(Path.GetTempPath(), $"TestEmails_{Guid.NewGuid()}");
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(false);
            _mockEnvironment.Setup(x => x.IsProduction()).Returns(true);

            // Mock для Directory.GetCurrentDirectory()
            var mockDirectory = new Mock<IDirectoryWrapper>();
            mockDirectory.Setup(d => d.GetCurrentDirectory()).Returns(_testEmailsDirectory);

            _service = new EmailService(
                _mockEmailSettings.Object,
                _mockLogger.Object,
                _mockEnvironment.Object,
                _mockConfiguration.Object);

            // Используем рефлексию для подмены путей
            var field = typeof(EmailService).GetField("_directoryWrapper",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_service, mockDirectory.Object);

            Directory.CreateDirectory(_testEmailsDirectory);
        }

        public void Dispose()
        {
            // Очищаем временные файлы
            if (Directory.Exists(_testEmailsDirectory))
            {
                Directory.Delete(_testEmailsDirectory, true);
            }
        }

        [Fact]
        public async Task SendEmailAsync_DevelopmentEnvironment_LogsEmailInfo()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
            _mockEnvironment.Setup(x => x.IsProduction()).Returns(false);

            var emailSettings = new EmailSettings
            {
                UseFileSystem = false,
                RequiresAuthentication = false
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var service = CreateService();
            var to = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act
            await service.SendEmailAsync(to, subject, body, false);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[DEV EMAIL]") &&
                                                  v.ToString().Contains(to) &&
                                                  v.ToString().Contains(subject)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_DevelopmentWithFileSystem_SavesEmailToFile()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
            _mockEnvironment.Setup(x => x.IsProduction()).Returns(false);

            var emailSettings = new EmailSettings
            {
                UseFileSystem = true,
                FileSystemPath = "test_emails",
                RequiresAuthentication = false
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var service = CreateService();
            var to = "test@example.com";
            var subject = "Test Subject";
            var body = "<p>Test HTML Body</p>";

            // Act
            await service.SendEmailAsync(to, subject, body, true);

            // Assert
            var emailDir = Path.Combine(_testEmailsDirectory, "test_emails");
            Assert.True(Directory.Exists(emailDir));

            var files = Directory.GetFiles(emailDir, "*.html");
            Assert.Single(files);

            var fileContent = await File.ReadAllTextAsync(files[0]);
            Assert.Contains(to, fileContent);
            Assert.Contains(subject, fileContent);
            Assert.Contains(body, fileContent);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email saved to file")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_ProductionEnvironment_SendsViaSmtp()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(false);
            _mockEnvironment.Setup(x => x.IsProduction()).Returns(true);

            var emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.test.com",
                SmtpPort = 587,
                UseSsl = true,
                SenderEmail = "sender@test.com",
                SenderName = "Test Sender",
                RequiresAuthentication = true,
                Username = "testuser",
                Password = "testpass"
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            var to = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act
            await service.SendEmailAsync(to, subject, body, false);

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email sent to") && v.ToString().Contains(to)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_RequiresAuthentication_UsesConfigurationCredentials()
        {
            // Arrange
            var emailSettings = new EmailSettings
            {
                RequiresAuthentication = true,
                Username = null, // Будет брать из конфигурации
                Password = null
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            _mockConfiguration.Setup(x => x["EmailSettings:Username"]).Returns("configuser");
            _mockConfiguration.Setup(x => x["EmailSettings:Password"]).Returns("configpass");

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "Body");

            // Assert
            mockSmtpClient.VerifySet(x => x.Credentials = It.Is<NetworkCredential>(c =>
                c.UserName == "configuser" &&
                c.Password == "configpass"), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_SmtpException_LogsErrorAndThrows()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            mockSmtpClient.Setup(x => x.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new SmtpException("SMTP error"));

            var service = CreateService(mockSmtpClient);
            var to = "test@example.com";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SmtpException>(() =>
                service.SendEmailAsync(to, "Test", "Body"));

            Assert.Contains("SMTP error", exception.Message);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send email") &&
                                                  v.ToString().Contains(to)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_AnyException_LogsErrorAndThrows()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            mockSmtpClient.Setup(x => x.SendMailAsync(It.IsAny<MailMessage>()))
                .ThrowsAsync(new InvalidOperationException("Some error"));

            var service = CreateService(mockSmtpClient);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendEmailAsync("test@example.com", "Test", "Body"));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_NonProductionWithoutAuthentication_LogsInfo()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
            _mockEnvironment.Setup(x => x.IsProduction()).Returns(false);

            var emailSettings = new EmailSettings
            {
                RequiresAuthentication = false,
                UseFileSystem = false
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var service = CreateService();

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "Body");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[DEV EMAIL]")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailConfirmationAsync_SendsFormattedEmail()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["App:BaseUrl"]).Returns("https://test.com");

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            var email = "user@example.com";
            var userName = "Test User";
            var confirmationLink = "https://test.com/confirm?token=abc123";

            // Act
            await service.SendEmailConfirmationAsync(email, userName, confirmationLink);

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.To[0].Address == email &&
                m.Subject == "Confirm your email - Smart Planner" &&
                m.Body.Contains(confirmationLink) &&
                m.Body.Contains(userName) &&
                m.IsBodyHtml
            )), Times.Once);
        }

        [Fact]
        public async Task SendPasswordResetAsync_SendsFormattedEmail()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            var email = "user@example.com";
            var userName = "Test User";
            var resetLink = "https://test.com/reset?token=xyz789";

            // Act
            await service.SendPasswordResetAsync(email, userName, resetLink);

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.To[0].Address == email &&
                m.Subject == "Reset your password - Smart Planner" &&
                m.Body.Contains(resetLink) &&
                m.Body.Contains(userName) &&
                m.IsBodyHtml
            )), Times.Once);
        }

        [Fact]
        public async Task SendWelcomeEmailAsync_SendsFormattedEmail()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["App:BaseUrl"]).Returns("https://test.com");

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            var email = "newuser@example.com";
            var userName = "New User";

            // Act
            await service.SendWelcomeEmailAsync(email, userName);

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.To[0].Address == email &&
                m.Subject.Contains("Welcome to Smart Planner") &&
                m.Body.Contains(userName) &&
                m.Body.Contains("https://test.com/dashboard") &&
                m.IsBodyHtml
            )), Times.Once);
        }

        [Fact]
        public async Task SendAccountLockedNotificationAsync_SendsFormattedEmail()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            var email = "user@example.com";
            var userName = "Test User";
            var lockoutEnd = DateTime.UtcNow.AddHours(1);

            // Act
            await service.SendAccountLockedNotificationAsync(email, userName, lockoutEnd);

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.To[0].Address == email &&
                m.Subject.Contains("Account Locked") &&
                m.Body.Contains(userName) &&
                m.Body.Contains(lockoutEnd.ToString("yyyy-MM-dd HH:mm:ss")) &&
                m.IsBodyHtml
            )), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_PlainTextEmail_SetsIsBodyHtmlFalse()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "Plain text body", false);

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                !m.IsBodyHtml
            )), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_HtmlEmail_SetsIsBodyHtmlTrue()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "<p>HTML body</p>", true);

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.IsBodyHtml
            )), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_SetsCustomHeaders()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "Body");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.Headers["X-SmartPlanner-Email-Type"] == "Transactional" &&
                !string.IsNullOrEmpty(m.Headers["X-SmartPlanner-App-Version"])
            )), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_WithCredentials_ConfiguresSmtpClient()
        {
            // Arrange
            var emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.test.com",
                SmtpPort = 465,
                UseSsl = false,
                RequiresAuthentication = true,
                Username = "testuser",
                Password = "testpass"
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "Body");

            // Assert
            mockSmtpClient.VerifySet(x => x.EnableSsl = false);
            mockSmtpClient.VerifySet(x => x.DeliveryMethod = SmtpDeliveryMethod.Network);
            mockSmtpClient.VerifySet(x => x.UseDefaultCredentials = false);
            mockSmtpClient.VerifySet(x => x.Host = "smtp.test.com");
            mockSmtpClient.VerifySet(x => x.Port = 465);
        }

        [Fact]
        public async Task SendEmailAsync_WithoutAuthentication_DoesNotSetCredentials()
        {
            // Arrange
            var emailSettings = new EmailSettings
            {
                RequiresAuthentication = false
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "Body");

            // Assert
            mockSmtpClient.VerifySet(x => x.Credentials = It.IsAny<NetworkCredential>(), Times.Never);
        }

        [Fact]
        public async Task SendEmailAsync_DevelopmentEnvironmentFileSystem_CreatesDirectory()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
            _mockEnvironment.Setup(x => x.IsProduction()).Returns(false);

            var emailSettings = new EmailSettings
            {
                UseFileSystem = true,
                FileSystemPath = "nested/test/emails",
                RequiresAuthentication = false
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var service = CreateService();
            var expectedDir = Path.Combine(_testEmailsDirectory, "nested/test/emails");

            // Act
            await service.SendEmailAsync("test@example.com", "Test", "Body");

            // Assert
            Assert.True(Directory.Exists(expectedDir));
        }

        [Fact]
        public async Task SendEmailAsync_FileSystemEmail_CreatesHtmlFile()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.IsDevelopment()).Returns(true);
            _mockEnvironment.Setup(x => x.IsProduction()).Returns(false);

            var emailSettings = new EmailSettings
            {
                UseFileSystem = true,
                FileSystemPath = "emails",
                RequiresAuthentication = false
            };
            _mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var service = CreateService();
            var to = "test@example.com";
            var subject = "Test Email";
            var body = "<h1>Test</h1>";

            // Act
            await service.SendEmailAsync(to, subject, body);

            // Assert
            var emailDir = Path.Combine(_testEmailsDirectory, "emails");
            var files = Directory.GetFiles(emailDir, "*.html");

            Assert.Single(files);
            var content = await File.ReadAllTextAsync(files[0]);

            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains(to, content);
            Assert.Contains(subject, content);
            Assert.Contains(body, content);
        }

        [Fact]
        public async Task SendEmailConfirmationAsync_UsesBaseUrlFromConfig()
        {
            // Arrange
            var baseUrl = "https://app.smartplanner.com";
            _mockConfiguration.Setup(x => x["App:BaseUrl"]).Returns(baseUrl);

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailConfirmationAsync("test@example.com", "User", "confirm-link");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.Body.Contains(baseUrl + "/unsubscribe") &&
                m.Body.Contains(baseUrl + "/privacy")
            )), Times.Once);
        }

        [Fact]
        public async Task SendWelcomeEmailAsync_UsesBaseUrlFromConfig()
        {
            // Arrange
            var baseUrl = "https://app.smartplanner.com";
            _mockConfiguration.Setup(x => x["App:BaseUrl"]).Returns(baseUrl);

            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendWelcomeEmailAsync("test@example.com", "User");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.Body.Contains(baseUrl + "/dashboard") &&
                m.Body.Contains(baseUrl + "/help") &&
                m.Body.Contains(baseUrl + "/unsubscribe") &&
                m.Body.Contains(baseUrl + "/contact")
            )), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_DoesNotThrow()
        {
            // Arrange
            var emailSettings = new EmailSettings();
            var mockEmailSettings = new Mock<IOptions<EmailSettings>>();
            mockEmailSettings.Setup(x => x.Value).Returns(emailSettings);

            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["EmailSettings:Username"]).Returns((string)null);
            mockConfiguration.Setup(x => x["EmailSettings:Password"]).Returns((string)null);

            // Act & Assert (не должно быть исключения)
            var service = new EmailService(
                mockEmailSettings.Object,
                Mock.Of<ILogger<EmailService>>(),
                Mock.Of<IHostEnvironment>(),
                mockConfiguration.Object);

            Assert.NotNull(service);
        }

        [Fact]
        public async Task SendEmailAsync_MultipleRecipientsInWelcomeEmail()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendWelcomeEmailAsync("user1@example.com,user2@example.com", "Test User");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.To.Count == 2 &&
                m.To[0].Address == "user1@example.com" &&
                m.To[1].Address == "user2@example.com"
            )), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_EmptyBody_SendsSuccessfully()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailAsync("test@example.com", "Empty Body Test", "");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_SpecialCharactersInSubject()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);
            var subject = "Test with special chars: & < > \" ' &amp;";

            // Act
            await service.SendEmailAsync("test@example.com", subject, "Body");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.Subject == subject
            )), Times.Once);
        }

        [Fact]
        public async Task SendEmailConfirmationAsync_NullUserName_HandlesGracefully()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailConfirmationAsync("test@example.com", null, "confirm-link");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendEmailConfirmationAsync_EmptyConfirmationLink()
        {
            // Arrange
            var mockSmtpClient = new Mock<ISmtpClient>();
            var service = CreateService(mockSmtpClient);

            // Act
            await service.SendEmailConfirmationAsync("test@example.com", "User", "");

            // Assert
            mockSmtpClient.Verify(x => x.SendMailAsync(It.Is<MailMessage>(m =>
                m.Body.Contains("confirm-link") == false
            )), Times.Once);
        }

        #region Вспомогательные методы

        private EmailService CreateService(Mock<ISmtpClient> mockSmtpClient = null)
        {
            var service = new EmailService(
                _mockEmailSettings.Object,
                _mockLogger.Object,
                _mockEnvironment.Object,
                _mockConfiguration.Object);

            // Используем рефлексию для инъекции моков
            if (mockSmtpClient != null)
            {
                var smtpClientField = typeof(EmailService).GetField("_smtpClient",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                smtpClientField?.SetValue(service, mockSmtpClient.Object);
            }

            return service;
        }

        #endregion
    }

    #region Вспомогательные интерфейсы и классы

    public interface ISmtpClient : IDisposable
    {
        string Host { get; set; }
        int Port { get; set; }
        bool EnableSsl { get; set; }
        SmtpDeliveryMethod DeliveryMethod { get; set; }
        bool UseDefaultCredentials { get; set; }
        ICredentialsByHost Credentials { get; set; }
        Task SendMailAsync(MailMessage message);
    }

    public interface IDirectoryWrapper
    {
        string GetCurrentDirectory();
    }

    #endregion
}
