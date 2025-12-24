namespace Calendarun.Common.Auth;

/// <summary>
/// Custom claim types used in the application
/// </summary>
public static class CalendarunClaimTypes
{
    /// <summary>
    /// Keycloak realm roles claim
    /// </summary>
    public const string RealmRoles = "realm_roles";

    /// <summary>
    /// Tenant ID header/claim
    /// </summary>
    public const string TenantId = "X-Tenant-Id";

    /// <summary>
    /// User's email claim
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// User's preferred username
    /// </summary>
    public const string PreferredUsername = "preferred_username";

    /// <summary>
    /// Subject (user ID) claim
    /// </summary>
    public const string Subject = "sub";
}

