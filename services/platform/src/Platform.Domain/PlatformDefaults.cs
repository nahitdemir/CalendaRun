namespace Platform.Domain;

/// <summary>
/// Default values and constants for the Platform domain
/// </summary>
public static class PlatformDefaults
{
    /// <summary>
    /// Default invite expiration in days (fallback when Settings not available)
    /// </summary>
    public const int DefaultInviteExpirationDays = 7;

    /// <summary>
    /// Default membership cache TTL in minutes (fallback when Settings not available)
    /// </summary>
    public const int DefaultMembershipCacheTtlMinutes = 5;

    /// <summary>
    /// Platform domain-specific Settings keys
    /// </summary>
    public static class SettingsKeys
    {
        /// <summary>
        /// Invite expiration in days setting key
        /// </summary>
        public const string InviteExpirationDays = "platform.invite.expiration_days";

        /// <summary>
        /// Membership cache TTL in minutes setting key
        /// </summary>
        public const string MembershipCacheTtlMinutes = "platform.membership.cache_ttl_minutes";
    }
}

