using Calendarun.Common;

namespace Planning.Domain;

/// <summary>
/// Default values and constants for the Planning domain
/// </summary>
public static class PlanningDefaults
{
    /// <summary>
    /// Default timezone when not specified by tenant settings
    /// Falls back to Defaults.Timezone if Settings not available
    /// </summary>
    public const string DefaultTimezone = Defaults.Timezone;

    /// <summary>
    /// Key used for global/tenant-less settings
    /// </summary>
    public const string GlobalSettingsKey = "global";

    /// <summary>
    /// Default email for unknown users (development only)
    /// </summary>
    public const string UnknownUserEmail = "unknown@local";

    /// <summary>
    /// Default maximum plans per user (fallback when Settings not available)
    /// </summary>
    public const int DefaultMaxPlansPerUser = 100;

    /// <summary>
    /// Planning domain-specific Settings keys
    /// These can be overridden per tenant via Settings service
    /// </summary>
    public static class SettingsKeys
    {
        /// <summary>
        /// Maximum plans per user setting key
        /// </summary>
        public const string MaxPlansPerUser = "planning.max_plans_per_user";

        /// <summary>
        /// Planning default timezone setting key
        /// Falls back to tenant.default_timezone, then Defaults.Timezone
        /// </summary>
        public const string DefaultTimezone = "planning.default_timezone";
    }
}

