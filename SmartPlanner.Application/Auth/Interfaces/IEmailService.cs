namespace SmartPlanner.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailConfirmationAsync(string email, string userName, string confirmationLink);
    Task SendPasswordResetAsync(string email, string userName, string resetLink);
    Task SendWelcomeEmailAsync(string email, string userName);
}
