namespace Calendarun.Common;

/// <summary>
/// Application-wide default values
/// </summary>
public static class Defaults
{
    /// <summary>
    /// Default language code
    /// </summary>
    public const string Language = "tr";

    /// <summary>
    /// Default currency code
    /// </summary>
    public const string Currency = "TRY";

    /// <summary>
    /// Default country code
    /// </summary>
    public const string CountryCode = "TR";

    /// <summary>
    /// Default timezone
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
}

