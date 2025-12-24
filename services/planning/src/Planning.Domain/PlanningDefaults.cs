namespace Planning.Domain;

/// <summary>
/// Default values and constants for the Planning domain
/// </summary>
public static class PlanningDefaults
{
    /// <summary>
    /// Default timezone when not specified by tenant settings
    /// </summary>
    public const string DefaultTimezone = "Europe/Istanbul";

    /// <summary>
    /// Key used for global/tenant-less settings
    /// </summary>
    public const string GlobalSettingsKey = "global";

    /// <summary>
    /// Default email for unknown users
    /// </summary>
    public const string UnknownUserEmail = "unknown@local";

    /// <summary>
    /// Default maximum plans per user
    /// </summary>
    public const int DefaultMaxPlansPerUser = 100;

    /// <summary>
    /// Settings keys
    /// </summary>
    public static class SettingsKeys
    {
        public const string MaxPlansPerUser = "planning.max_plans_per_user";
        public const string DefaultTimezone = "planning.default_timezone";
    }
}

