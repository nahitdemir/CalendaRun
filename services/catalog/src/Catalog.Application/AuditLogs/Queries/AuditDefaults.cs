namespace Catalog.Application.AuditLogs.Queries;

/// <summary>
/// Default values for audit log queries
/// </summary>
public static class AuditDefaults
{
    /// <summary>
    /// Default page size for audit log queries (fallback when Settings not available)
    /// </summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// Settings key for default page size
    /// </summary>
    public const string SettingsKey = "audit.default_page_size";
}

