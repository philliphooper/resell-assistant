using Resell_Assistant.Models;

namespace Resell_Assistant.Services.External;

/// <summary>
/// Interface for eBay API integration service
/// Handles authentication, product search, and data retrieval from eBay
/// </summary>
public interface IEbayApiService
{
    /// <summary>
    /// Search for products on eBay marketplace
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="priceMin">Minimum price filter</param>
    /// <param name="priceMax">Maximum price filter</param>
    /// <param name="condition">Item condition filter (New, Used, etc.)</param>
    /// <param name="marketplace">eBay marketplace (EBAY_US, EBAY_GB, etc.)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>List of products found on eBay</returns>
    Task<List<Product>> SearchProductsAsync(
        string query,
        string? category = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        string? condition = null,
        string? marketplace = null,
        int limit = 50);

    /// <summary>
    /// Get detailed information for a specific eBay item
    /// </summary>
    /// <param name="itemId">eBay item ID</param>
    /// <returns>Detailed product information</returns>
    Task<Product?> GetProductDetailsAsync(string itemId);

    /// <summary>
    /// Get sold listings for price analysis and market research
    /// </summary>
    /// <param name="query">Search query for sold items</param>
    /// <param name="days">Number of days to look back (default 30)</param>
    /// <param name="limit">Maximum number of sold listings to return</param>
    /// <returns>List of sold listings with prices and dates</returns>
    Task<List<SoldListing>> GetSoldListingsAsync(string query, int days = 30, int limit = 50);

    /// <summary>
    /// Get price history data for market analysis
    /// </summary>
    /// <param name="query">Product search query</param>
    /// <param name="days">Number of days of history to retrieve</param>
    /// <returns>List of price points over time</returns>
    Task<List<PricePoint>> GetPriceHistoryAsync(string query, int days = 90);

    /// <summary>
    /// Get product categories available on eBay
    /// </summary>
    /// <param name="parentCategoryId">Optional parent category to get subcategories</param>
    /// <returns>List of available categories</returns>
    Task<List<Category>> GetCategoriesAsync(string? parentCategoryId = null);

    /// <summary>
    /// Check if the eBay API service is properly configured and accessible
    /// </summary>
    /// <returns>True if service is operational, false otherwise</returns>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Get current API rate limit status
    /// </summary>
    /// <returns>Rate limit information including remaining calls</returns>
    Task<ApiRateLimitInfo> GetRateLimitStatusAsync();
}

/// <summary>
/// Represents a sold listing from eBay for price analysis
/// </summary>
public class SoldListing
{
    public string ItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal SoldPrice { get; set; }
    public DateTime SoldDate { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string Marketplace { get; set; } = string.Empty;
    public bool BestOffer { get; set; }
    public int BidCount { get; set; }
}

/// <summary>
/// Represents a price point for historical price analysis
/// </summary>
public class PricePoint
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string Source { get; set; } = string.Empty;
    public int SampleSize { get; set; }
}

/// <summary>
/// Represents a product category from eBay
/// </summary>
public class Category
{
    public string CategoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ParentCategoryId { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool HasChildren { get; set; }
}

/// <summary>
/// API rate limit information
/// </summary>
public class ApiRateLimitInfo
{
    public int RemainingCalls { get; set; }
    public int DailyLimit { get; set; }
    public int PerSecondLimit { get; set; }
    public DateTime ResetTime { get; set; }
    public bool IsLimited { get; set; }
}
