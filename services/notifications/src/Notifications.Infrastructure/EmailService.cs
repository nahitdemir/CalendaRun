using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Notifications.Infrastructure;

public class EmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _fromEmail;

    public EmailService(string smtpHost, int smtpPort, string fromEmail)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _fromEmail = fromEmail;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("CalendaRun", _fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();
        // Mailhog does not use TLS or authentication
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.None, ct);
        // No authentication needed for Mailhog
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}

