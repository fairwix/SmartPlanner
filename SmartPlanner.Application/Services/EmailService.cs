using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SmartPlanner.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            IHostEnvironment environment,
            IConfiguration configuration)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                if (_environment.IsDevelopment() && _emailSettings.UseFileSystem)
                {
                    await SaveEmailToFileSystemAsync(to, subject, body);
                    return;
                }

                if (_environment.IsProduction() || _emailSettings.RequiresAuthentication)
                {
                    await SendEmailViaSmtpAsync(to, subject, body, isHtml);
                }
                else
                {
                    _logger.LogInformation("[DEV EMAIL] To: {To}, Subject: {Subject}", to, subject);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }

        private async Task SendEmailViaSmtpAsync(string to, string subject, string body, bool isHtml)
        {
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (_emailSettings.RequiresAuthentication)
            {
                client.Credentials = new NetworkCredential(
                    _emailSettings.Username ?? _configuration["EmailSettings:Username"],
                    _emailSettings.Password ?? _configuration["EmailSettings:Password"]
                );
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);

            mailMessage.Headers.Add("X-SmartPlanner-Email-Type", "Transactional");
            mailMessage.Headers.Add("X-SmartPlanner-App-Version", GetType().Assembly.GetName().Version?.ToString() ?? "1.0");

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent to {To} via SMTP", to);
        }

        private async Task SaveEmailToFileSystemAsync(string to, string subject, string body)
        {
            var emailPath = Path.Combine(Directory.GetCurrentDirectory(), _emailSettings.FileSystemPath);
            Directory.CreateDirectory(emailPath);

            var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Guid.NewGuid():N}.html";
            var filePath = Path.Combine(emailPath, fileName);

            var emailContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Email Preview: {subject}</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 20px; }}
                        .header {{ background: #f0f0f0; padding: 10px; border-radius: 5px; }}
                        .to {{ color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>{subject}</h2>
                        <div class='to'><strong>To:</strong> {to}</div>
                        <div><strong>Date:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>
                    </div>
                    <hr>
                    <div class='content'>
                        {body}
                    </div>
                </body>
                </html>
            ";

            await File.WriteAllTextAsync(filePath, emailContent);
            _logger.LogInformation("Email saved to file: {FilePath}", filePath);
        }

        public async Task SendEmailConfirmationAsync(string email, string userName, string confirmationLink)
        {
            var subject = "Confirm your email - Smart Planner";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white;'>
                        <h1 style='margin: 0;'>Welcome to Smart Planner!</h1>
                    </div>

                    <div style='padding: 30px; background: #f9f9f9;'>
                        <h2>Hi {userName},</h2>
                        <p>Thank you for registering with Smart Planner. To complete your registration, please confirm your email address by clicking the button below:</p>

                        <div style='text-align: center; margin: 40px 0;'>
                            <a href='{confirmationLink}'
                               style='background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>
                               Confirm Email Address
                            </a>
                        </div>

                        <p>Or copy and paste this link into your browser:</p>
                        <p style='background: #eee; padding: 10px; border-radius: 5px; word-break: break-all;'>
                            {confirmationLink}
                        </p>

                        <p>This link will expire in 24 hours.</p>

                        <div style='margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; color: #666;'>
                            <p>If you didn't create an account, you can safely ignore this email.</p>
                            <p>Need help? Contact our support team at support@smartplanner.com</p>
                        </div>
                    </div>

                    <div style='background: #333; color: white; padding: 20px; text-align: center;'>
                        <p>&copy; {DateTime.Now.Year} Smart Planner. All rights reserved.</p>
                        <p style='font-size: 12px;'>
                            This email was sent to {email}.<br>
                            <a href='{_configuration["App:BaseUrl"]}/unsubscribe' style='color: #aaa;'>Unsubscribe</a> |
                            <a href='{_configuration["App:BaseUrl"]}/privacy' style='color: #aaa;'>Privacy Policy</a>
                        </p>
                    </div>
                </div>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetAsync(string email, string userName, string resetLink)
        {
            var subject = "Reset your password - Smart Planner";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; text-align: center; color: white;'>
                        <h1 style='margin: 0;'>Password Reset</h1>
                    </div>

                    <div style='padding: 30px; background: #f9f9f9;'>
                        <h2>Hello {userName},</h2>
                        <p>We received a request to reset your password. Click the button below to set a new password:</p>

                        <div style='text-align: center; margin: 40px 0;'>
                            <a href='{resetLink}'
                               style='background: #f5576c; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>
                               Reset Password
                            </a>
                        </div>

                        <p>Or use this link:</p>
                        <p style='background: #eee; padding: 10px; border-radius: 5px; word-break: break-all;'>
                            {resetLink}
                        </p>

                        <p><strong>⚠️ This link expires in 24 hours.</strong></p>

                        <div style='background: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 15px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>Security Tip:</strong> If you didn't request this password reset, please ignore this email and consider changing your password immediately.</p>
                        </div>

                        <p>For security reasons, we recommend:</p>
                        <ul>
                            <li>Use a strong password (mix of letters, numbers, and symbols)</li>
                            <li>Don't reuse passwords across different services</li>
                            <li>Enable two-factor authentication if available</li>
                        </ul>
                    </div>
                </div>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string email, string userName)
        {
            var subject = "🎉 Welcome to Smart Planner!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%); padding: 40px; text-align: center; color: white;'>
                        <h1 style='margin: 0 0 10px 0;'>Welcome aboard, {userName}!</h1>
                        <p style='font-size: 18px; opacity: 0.9;'>We're excited to help you achieve your goals</p>
                    </div>

                    <div style='padding: 30px; background: #f9f9f9;'>
                        <h2 style='color: #4facfe;'>Let's Get Started</h2>

                        <div style='display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; margin: 30px 0;'>
                            <div style='background: white; padding: 20px; border-radius: 10px; text-align: center; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                <div style='font-size: 36px; margin-bottom: 10px;'>🎯</div>
                                <h3 style='margin: 0 0 10px 0;'>Create Your First Goal</h3>
                                <p style='color: #666;'>Start by setting a SMART goal</p>
                            </div>

                            <div style='background: white; padding: 20px; border-radius: 10px; text-align: center; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                <div style='font-size: 36px; margin-bottom: 10px;'>🏆</div>
                                <h3 style='margin: 0 0 10px 0;'>Join Challenges</h3>
                                <p style='color: #666;'>Compete with friends and earn rewards</p>
                            </div>

                            <div style='background: white; padding: 20px; border-radius: 10px; text-align: center; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                <div style='font-size: 36px; margin-bottom: 10px;'>👥</div>
                                <h3 style='margin: 0 0 10px 0;'>Connect with Friends</h3>
                                <p style='color: #666;'>Add friends and stay motivated together</p>
                            </div>

                            <div style='background: white; padding: 20px; border-radius: 10px; text-align: center; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                <div style='font-size: 36px; margin-bottom: 10px;'>📊</div>
                                <h3 style='margin: 0 0 10px 0;'>Track Progress</h3>
                                <p style='color: #666;'>Monitor your achievements and streaks</p>
                            </div>
                        </div>

                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{_configuration["App:BaseUrl"]}/dashboard'
                               style='background: #4facfe; color: white; padding: 15px 40px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; font-size: 16px;'>
                               Go to Dashboard
                            </a>
                        </div>

                        <div style='background: #e8f4fd; border-left: 4px solid #4facfe; padding: 15px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>Pro Tip:</strong> Download our mobile app to track goals on the go!</p>
                        </div>
                    </div>

                    <div style='background: #333; color: white; padding: 20px; text-align: center;'>
                        <p>Need help getting started? <a href='{_configuration["App:BaseUrl"]}/help' style='color: #4facfe;'>Visit our help center</a></p>
                        <p style='font-size: 12px; color: #aaa;'>
                            You're receiving this email because you registered at Smart Planner.<br>
                            <a href='{_configuration["App:BaseUrl"]}/unsubscribe' style='color: #888;'>Unsubscribe</a> |
                            <a href='{_configuration["App:BaseUrl"]}/contact' style='color: #888;'>Contact Support</a>
                        </p>
                    </div>
                </div>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendAccountLockedNotificationAsync(string email, string userName, DateTime lockoutEnd)
        {
            var subject = "⚠️ Account Locked - Smart Planner";
            var body = $@"
                <h2>Account Security Alert</h2>
                <p>Hello {userName},</p>
                <p>Your account has been temporarily locked due to multiple failed login attempts.</p>
                <p><strong>Lockout ends:</strong> {lockoutEnd:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p>If this wasn't you, please contact our support team immediately.</p>
            ";

            await SendEmailAsync(email, subject, body);
        }
    }
}
