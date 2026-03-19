using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Utility.EmailSender;

public class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _senderEmail;
    private readonly string _senderPassword;
    private readonly ILogger<EmailService>? _logger;

    public EmailService(string senderEmail, string senderPassword, string smtpHost, int smtpPort, ILogger<EmailService>? logger = null)
    {
        _senderEmail = senderEmail;
        _senderPassword = senderPassword;
        _host = smtpHost;
        _port = smtpPort;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_senderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_host, _port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_senderEmail, _senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger?.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Email failed to {Recipient} with subject {Subject}", to, subject);
            throw;
        }
    }
}
