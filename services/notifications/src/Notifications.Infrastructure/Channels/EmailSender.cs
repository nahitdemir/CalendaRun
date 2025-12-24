using System.Diagnostics;
using Calendarun.Settings.Client;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Notifications.Domain;

namespace Notifications.Infrastructure.Channels;

public class EmailSender : IChannelSender
{
    private readonly ISettingsClient _settingsClient;
    private readonly ILogger<EmailSender> _logger;

    // Fallback defaults
    private const string DefaultSmtpHost = "localhost";
    private const int DefaultSmtpPort = 1025;
    private const string DefaultFromEmail = "noreply@calendarun.local";

    public EmailSender(ISettingsClient settingsClient, ILogger<EmailSender> logger)
    {
        _settingsClient = settingsClient;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;
    public string ProviderName => "MailKit-SMTP";

    public async Task<ChannelSendResult> SendAsync(NotificationJob job, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tenantIdStr = job.TenantId?.ToString();
        
        try
        {
            // Get SMTP settings from settings service (tenant-aware)
            var smtpHost = await _settingsClient.GetAsync<string>("notifications.smtp.host", tenantIdStr, ct) ?? DefaultSmtpHost;
            var smtpPort = await _settingsClient.GetAsync<int?>("notifications.smtp.port", tenantIdStr, ct) ?? DefaultSmtpPort;
            var fromEmail = await _settingsClient.GetAsync<string>("notifications.smtp.from", tenantIdStr, ct) ?? DefaultFromEmail;

            if (string.IsNullOrEmpty(job.RecipientAddress))
            {
                return new ChannelSendResult(false, "Recipient address is empty", (int)sw.ElapsedMilliseconds);
            }

            _logger.LogDebug("📧 SMTP Config: Host={Host}, Port={Port}, From={From}", smtpHost, smtpPort, fromEmail);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CalendaRun", fromEmail));
            message.To.Add(MailboxAddress.Parse(job.RecipientAddress));
            message.Subject = job.Subject ?? "Notification";

            message.Body = new TextPart("plain")
            {
                Text = job.Body ?? ""
            };

            using var client = new SmtpClient();
            // Mailhog does not use TLS or authentication
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.None, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            sw.Stop();
            _logger.LogInformation("✅ Email sent to {Recipient} in {Duration}ms", job.RecipientAddress, sw.ElapsedMilliseconds);
            
            return new ChannelSendResult(true, null, (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "❌ Email send failed to {Recipient}", job.RecipientAddress);
            return new ChannelSendResult(false, ex.Message, (int)sw.ElapsedMilliseconds);
        }
    }
}
