using Calendarun.Settings.Client;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Notifications.Infrastructure;

public class EmailService
{
    private readonly ISettingsClient _settingsClient;
    private readonly ILogger<EmailService> _logger;
    
    // Fallback defaults
    private const string DefaultSmtpHost = "localhost";
    private const int DefaultSmtpPort = 1025;
    private const string DefaultFromEmail = "noreply@calendarun.local";

    public EmailService(ISettingsClient settingsClient, ILogger<EmailService> logger)
    {
        _settingsClient = settingsClient;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        // Get SMTP settings from settings service
        var smtpHost = await _settingsClient.GetAsync<string>("notifications.smtp.host", null, ct) ?? DefaultSmtpHost;
        var smtpPort = await _settingsClient.GetAsync<int?>("notifications.smtp.port", null, ct) ?? DefaultSmtpPort;
        var fromEmail = await _settingsClient.GetAsync<string>("notifications.smtp.from", null, ct) ?? DefaultFromEmail;

        _logger.LogDebug("📧 SMTP Config: Host={Host}, Port={Port}, From={From}", smtpHost, smtpPort, fromEmail);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("CalendaRun", fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();
        // Mailhog does not use TLS or authentication
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.None, ct);
        // No authentication needed for Mailhog
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    public async Task SendTemplatedEmailAsync(
        string toEmail, 
        Guid eventId, 
        Guid planItemId, 
        CancellationToken ct = default)
    {
        // Get templates from settings
        var subjectTemplate = await _settingsClient.GetAsync<string>("notifications.email.subject_template", null, ct) 
            ?? "You planned event: {EventId}";
        var bodyTemplate = await _settingsClient.GetAsync<string>("notifications.email.body_template", null, ct)
            ?? "Hello!\n\nYou have planned event {EventId}.\nPlan ID: {PlanItemId}\n\nBest regards,\nCalendaRun Team";

        // Replace placeholders
        var subject = subjectTemplate
            .Replace("{EventId}", eventId.ToString());
        
        var body = bodyTemplate
            .Replace("{EventId}", eventId.ToString())
            .Replace("{PlanItemId}", planItemId.ToString());

        await SendEmailAsync(toEmail, subject, body, ct);
    }
}
