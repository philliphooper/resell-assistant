namespace Resell_Assistant.Models.Configuration;

/// <summary>
/// Configuration settings for eBay API integration
/// </summary>
public class EbayApiSettings
{
    /// <summary>
    /// eBay Application Client ID (from Developer Portal)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// eBay Application Client Secret (from Developer Portal)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Environment: "sandbox" or "production"
    /// </summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Base URL for eBay API calls
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.sandbox.ebay.com";

    /// <summary>
    /// OAuth URL for token generation
    /// </summary>
    public string OAuthUrl { get; set; } = "https://auth.sandbox.ebay.com/oauth/api_scope";

    /// <summary>
    /// Rate limiting: maximum calls per second
    /// </summary>
    public int RateLimitPerSecond { get; set; } = 5;

    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// OAuth token expiration buffer (minutes before expiry to refresh)
    /// </summary>
    public int TokenRefreshBufferMinutes { get; set; } = 10;

    /// <summary>
    /// Default marketplace for searches (EBAY_US, EBAY_GB, etc.)
    /// </summary>
    public string DefaultMarketplace { get; set; } = "EBAY_US";

    /// <summary>
    /// Maximum number of items to fetch per API call
    /// </summary>
    public int MaxItemsPerRequest { get; set; } = 50;

    /// <summary>
    /// Validate that all required settings are provided
    /// </summary>
    public bool IsValid => 
        !string.IsNullOrEmpty(ClientId) && 
        !string.IsNullOrEmpty(ClientSecret) && 
        !string.IsNullOrEmpty(BaseUrl) && 
        !string.IsNullOrEmpty(OAuthUrl);
}
