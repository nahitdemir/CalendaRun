namespace Gateway;

/// <summary>
/// Default values and constants for Gateway
/// </summary>
public static class GatewayDefaults
{
    /// <summary>
    /// Default HTTP client timeout in seconds (fallback when Settings not available)
    /// </summary>
    public const int DefaultHttpClientTimeoutSeconds = 5;

    /// <summary>
    /// Settings key for HTTP client timeout
    /// </summary>
    public const string SettingsKey = "gateway.http_client.timeout_seconds";
}

