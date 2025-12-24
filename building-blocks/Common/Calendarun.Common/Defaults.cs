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
    /// These can be overridden per tenant via Settings service
    /// </summary>
    public static class SettingsKeys
    {
        /// <summary>
        /// Tenant default language setting key
        /// </summary>
        public const string TenantDefaultLanguage = "tenant.default_language";

        /// <summary>
        /// Tenant default currency setting key
        /// </summary>
        public const string TenantDefaultCurrency = "tenant.default_currency";

        /// <summary>
        /// Tenant default country code setting key
        /// </summary>
        public const string TenantDefaultCountryCode = "tenant.default_country_code";

        /// <summary>
        /// Tenant default timezone setting key
        /// </summary>
        public const string TenantDefaultTimezone = "tenant.default_timezone";
    }
}

