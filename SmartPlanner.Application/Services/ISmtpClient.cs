using System.Net;
using System.Net.Mail;

namespace SmartPlanner.Application.Services;

public interface IDirectoryWrapper
{
    string GetCurrentDirectory();
}

internal sealed class SystemDirectoryWrapper : IDirectoryWrapper
{
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
}

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

internal sealed class SystemSmtpClient : ISmtpClient
{
    private readonly SmtpClient _client = new();

    public string Host { get => _client.Host; set => _client.Host = value; }
    public int Port { get => _client.Port; set => _client.Port = value; }
    public bool EnableSsl { get => _client.EnableSsl; set => _client.EnableSsl = value; }
    public SmtpDeliveryMethod DeliveryMethod { get => _client.DeliveryMethod; set => _client.DeliveryMethod = value; }
    public bool UseDefaultCredentials { get => _client.UseDefaultCredentials; set => _client.UseDefaultCredentials = value; }
    public ICredentialsByHost Credentials { get => _client.Credentials!; set => _client.Credentials = value; }
    public Task SendMailAsync(MailMessage message) => _client.SendMailAsync(message);
    public void Dispose() => _client.Dispose();
}
