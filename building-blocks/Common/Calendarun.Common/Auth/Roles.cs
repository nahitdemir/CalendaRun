namespace Calendarun.Common.Auth;

/// <summary>
/// Application-wide role constants
/// </summary>
public static class Roles
{
    /// <summary>
    /// Super admin role - full system access
    /// </summary>
    public const string SuperAdmin = "super_admin";
}

/// <summary>
/// Tenant-level roles
/// </summary>
public enum TenantRole
{
    /// <summary>
    /// Regular tenant member
    /// </summary>
    TenantUser,

    /// <summary>
    /// Tenant administrator with management access
    /// </summary>
    TenantAdmin
}

/// <summary>
/// Extension methods for TenantRole
/// </summary>
public static class TenantRoleExtensions
{
    /// <summary>
    /// Converts string to TenantRole enum
    /// </summary>
    public static TenantRole ToTenantRole(this string? value)
    {
        return Enum.TryParse<TenantRole>(value, ignoreCase: true, out var result)
            ? result
            : TenantRole.TenantUser;
    }

    /// <summary>
    /// Checks if role has admin privileges
    /// </summary>
    public static bool IsAdmin(this TenantRole role)
    {
        return role == TenantRole.TenantAdmin;
    }
}

