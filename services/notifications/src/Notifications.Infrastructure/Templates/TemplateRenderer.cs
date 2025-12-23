using System.Text.Json;
using Calendarun.Settings.Client;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.Templates;

public interface ITemplateRenderer
{
    Task<(string Subject, string Body)> RenderAsync(
        string eventType,
        string payloadJson,
        string? tenantId = null,
        CancellationToken ct = default);
}

public class TemplateRenderer : ITemplateRenderer
{
    private readonly ISettingsClient _settingsClient;
    private readonly ILogger<TemplateRenderer> _logger;

    // Default templates
    private static readonly Dictionary<string, (string Subject, string Body)> DefaultTemplates = new()
    {
        ["PlanningUserPlannedV1"] = (
            "You planned event: {EventId}",
            "Hello!\n\nYou have planned event {EventId}.\nPlan ID: {PlanItemId}\n\nBest regards,\nCalendaRun Team"
        ),
        ["ReminderNotification"] = (
            "Reminder: Event {EventId} starts soon!",
            "Hello!\n\nThis is a reminder that your planned event {EventId} starts soon.\n\nBest regards,\nCalendaRun Team"
        )
    };

    public TemplateRenderer(ISettingsClient settingsClient, ILogger<TemplateRenderer> logger)
    {
        _settingsClient = settingsClient;
        _logger = logger;
    }

    public async Task<(string Subject, string Body)> RenderAsync(
        string eventType,
        string payloadJson,
        string? tenantId = null,
        CancellationToken ct = default)
    {
        // Get templates from settings or use defaults
        var subjectTemplate = await GetTemplateAsync($"notifications.template.{eventType.ToLower()}.subject", tenantId, ct);
        var bodyTemplate = await GetTemplateAsync($"notifications.template.{eventType.ToLower()}.body", tenantId, ct);

        // Fallback to generic templates
        if (string.IsNullOrEmpty(subjectTemplate) || string.IsNullOrEmpty(bodyTemplate))
        {
            subjectTemplate = await _settingsClient.GetAsync<string>("notifications.email.subject_template", tenantId, ct);
            bodyTemplate = await _settingsClient.GetAsync<string>("notifications.email.body_template", tenantId, ct);
        }

        // Fallback to hardcoded defaults
        if (string.IsNullOrEmpty(subjectTemplate) || string.IsNullOrEmpty(bodyTemplate))
        {
            if (DefaultTemplates.TryGetValue(eventType, out var defaults))
            {
                subjectTemplate ??= defaults.Subject;
                bodyTemplate ??= defaults.Body;
            }
            else
            {
                subjectTemplate ??= "Notification";
                bodyTemplate ??= "You have a new notification.";
            }
        }

        // Parse payload and replace placeholders
        var placeholders = ParsePayload(payloadJson);
        var subject = ReplacePlaceholders(subjectTemplate, placeholders);
        var body = ReplacePlaceholders(bodyTemplate, placeholders);

        _logger.LogDebug("Template rendered for {EventType}: Subject={Subject}", eventType, subject);

        return (subject, body);
    }

    private async Task<string?> GetTemplateAsync(string key, string? tenantId, CancellationToken ct)
    {
        try
        {
            return await _settingsClient.GetAsync<string>(key, tenantId, ct);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string> ParsePayload(string payloadJson)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? "",
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => prop.Value.GetRawText()
                };
                result[prop.Name] = value;
            }
        }
        catch
        {
            // If parsing fails, return empty dictionary
        }

        return result;
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var (key, value) in placeholders)
        {
            result = result.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }
}

