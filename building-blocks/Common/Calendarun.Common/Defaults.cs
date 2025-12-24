namespace Calendarun.Common;

/// <summary>
/// Application-wide default values
/// These are fallback values when Settings are not available or not configured
/// </summary>
public static class Defaults
{
    /// <summary>
    /// Default language code (fallback)
    /// </summary>
    public const string Language = "tr";

    /// <summary>
    /// Default currency code (fallback)
    /// </summary>
    public const string Currency = "TRY";

    /// <summary>
    /// Default country code (fallback)
    /// </summary>
    public const string CountryCode = "TR";

    /// <summary>
    /// Default timezone (fallback)
    /// </summary>
    public const string Timezone = "Europe/Istanbul";

    /// <summary>
    /// Supported languages
    /// </summary>
    public static class Languages
    {
        public const string Turkish = "tr";
        public const string English = "en";

        public static readonly string[] Supported = [Turkish, English];
    }

    /// <summary>
    /// Supported currencies
    /// </summary>
    public static class Currencies
    {
        public const string TurkishLira = "TRY";
        public const string USDollar = "USD";
        public const string Euro = "EUR";

        public static readonly string[] Supported = [TurkishLira, USDollar, Euro];
    }

    /// <summary>
    /// Settings keys for tenant-specific configuration
    /// Note: Tenant.DefaultLanguage and Tenant.DefaultCurrency are stored in Tenant entity,
    /// not in Settings, for performance reasons (read on every request).
    /// </summary>
    public static class SettingsKeys
    {
        // No tenant-general settings keys here.
        // Domain-specific settings (e.g., planning.*) are defined in their respective domains.
    }
}

