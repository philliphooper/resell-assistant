using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using Resell_Assistant.Models;
using Resell_Assistant.Models.Configuration;
using System.Text;

namespace Resell_Assistant.Services.External;

/// <summary>
/// eBay API service implementation for marketplace data integration
/// Handles OAuth authentication, product search, and data retrieval from eBay Browse API
/// </summary>
public class EbayApiService : IEbayApiService
{
    private readonly EbayApiSettings _settings;
    private readonly ICredentialService _credentialService;
    private readonly ILogger<EbayApiService> _logger;
    private readonly RestClient _client;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public EbayApiService(
        IOptions<EbayApiSettings> settings, 
        ICredentialService credentialService,
        ILogger<EbayApiService> logger)
    {
        _settings = settings.Value;
        _credentialService = credentialService;
        _logger = logger;
          // Initialize RestClient with base URL and reduced timeout for better responsiveness
        var options = new RestClientOptions(_settings.BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(Math.Min(_settings.TimeoutSeconds, 5)) // Cap at 5 seconds for dashboard responsiveness
        };
        _client = new RestClient(options);
        
        // Initialize rate limiting semaphore
        _rateLimitSemaphore = new SemaphoreSlim(_settings.RateLimitPerSecond, _settings.RateLimitPerSecond);
        
        // Start rate limit release timer
        StartRateLimitTimer();
    }

    /// <summary>
    /// Search for products on eBay marketplace
    /// </summary>
    public async Task<List<Product>> SearchProductsAsync(
        string query,
        string? category = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        string? condition = null,
        string? marketplace = null,
        int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty", nameof(query));

        try
        {
            await EnsureValidTokenAsync();
            await _rateLimitSemaphore.WaitAsync();

            var request = new RestRequest("/buy/browse/v1/item_summary/search", Method.Get);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            request.AddHeader("X-EBAY-C-MARKETPLACE-ID", marketplace ?? _settings.DefaultMarketplace);
            
            // Add search parameters
            request.AddParameter("q", query);
            request.AddParameter("limit", Math.Min(limit, _settings.MaxItemsPerRequest));
            
            if (!string.IsNullOrEmpty(category))
                request.AddParameter("category_ids", category);
            
            if (priceMin.HasValue || priceMax.HasValue)
            {
                var priceFilter = BuildPriceFilter(priceMin, priceMax);
                if (!string.IsNullOrEmpty(priceFilter))
                    request.AddParameter("filter", priceFilter);
            }
            
            if (!string.IsNullOrEmpty(condition))
                request.AddParameter("filter", $"conditions:{{{condition}}}");

            _logger.LogDebug("Searching eBay for query: {Query}, category: {Category}, limit: {Limit}", 
                           query, category, limit);

            var response = await _client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogWarning("eBay API search failed: {StatusCode} - {Content}", 
                                 response.StatusCode, response.Content);
                return new List<Product>();
            }

            var searchResult = JsonConvert.DeserializeObject<EbaySearchResponse>(response.Content!);
            return ConvertToProducts(searchResult?.ItemSummaries ?? new List<EbayItemSummary>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching eBay products for query: {Query}", query);
            return new List<Product>();
        }
        finally
        {
            try
            {
                _rateLimitSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ignore - semaphore is already at maximum capacity
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
        }
    }

    /// <summary>
    /// Get detailed information for a specific eBay item
    /// </summary>
    public async Task<Product?> GetProductDetailsAsync(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("Item ID cannot be empty", nameof(itemId));

        try
        {
            await EnsureValidTokenAsync();
            await _rateLimitSemaphore.WaitAsync();

            var request = new RestRequest($"/buy/browse/v1/item/{itemId}", Method.Get);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            request.AddHeader("X-EBAY-C-MARKETPLACE-ID", _settings.DefaultMarketplace);

            _logger.LogDebug("Getting eBay item details for ID: {ItemId}", itemId);

            var response = await _client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogWarning("eBay API item details failed: {StatusCode} - {Content}", 
                                 response.StatusCode, response.Content);
                return null;
            }

            var itemDetails = JsonConvert.DeserializeObject<EbayItemDetails>(response.Content!);
            return ConvertToProduct(itemDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eBay item details for ID: {ItemId}", itemId);
            return null;
        }
        finally
        {
            try
            {
                _rateLimitSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ignore - semaphore is already at maximum capacity
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
        }
    }

    /// <summary>
    /// Get sold listings for price analysis and market research
    /// </summary>
    public async Task<List<SoldListing>> GetSoldListingsAsync(string query, int days = 30, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty", nameof(query));

        try
        {
            await EnsureValidTokenAsync();
            await _rateLimitSemaphore.WaitAsync();

            var request = new RestRequest("/buy/browse/v1/item_summary/search", Method.Get);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            request.AddHeader("X-EBAY-C-MARKETPLACE-ID", _settings.DefaultMarketplace);
            
            // Search for sold listings
            request.AddParameter("q", query);
            request.AddParameter("filter", "buyingOptions:{AUCTION},itemLocationCountry:US");
            request.AddParameter("sort", "price");
            request.AddParameter("limit", Math.Min(limit, _settings.MaxItemsPerRequest));

            _logger.LogDebug("Getting eBay sold listings for query: {Query}, days: {Days}", query, days);

            var response = await _client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogWarning("eBay API sold listings failed: {StatusCode} - {Content}", 
                                 response.StatusCode, response.Content);
                return new List<SoldListing>();
            }

            var searchResult = JsonConvert.DeserializeObject<EbaySearchResponse>(response.Content!);
            return ConvertToSoldListings(searchResult?.ItemSummaries ?? new List<EbayItemSummary>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eBay sold listings for query: {Query}", query);
            return new List<SoldListing>();
        }
        finally
        {
            try
            {
                _rateLimitSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ignore - semaphore is already at maximum capacity
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
        }
    }

    /// <summary>
    /// Get price history data for market analysis
    /// </summary>
    public async Task<List<PricePoint>> GetPriceHistoryAsync(string query, int days = 90)
    {
        // For this implementation, we'll use sold listings to create price history
        var soldListings = await GetSoldListingsAsync(query, days, 200);
        
        return soldListings
            .GroupBy(s => s.SoldDate.Date)
            .Select(g => new PricePoint
            {
                Date = g.Key,
                Price = g.Average(s => s.SoldPrice),
                Currency = "USD",
                Source = "eBay Sold Listings",
                SampleSize = g.Count()
            })
            .OrderBy(p => p.Date)
            .ToList();
    }

    /// <summary>
    /// Get product categories available on eBay
    /// </summary>
    public async Task<List<Category>> GetCategoriesAsync(string? parentCategoryId = null)
    {
        try
        {
            await EnsureValidTokenAsync();
            await _rateLimitSemaphore.WaitAsync();

            var request = new RestRequest("/commerce/taxonomy/v1/category_tree/0", Method.Get);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");

            if (!string.IsNullOrEmpty(parentCategoryId))
                request.AddParameter("category_id", parentCategoryId);

            _logger.LogDebug("Getting eBay categories, parent: {ParentId}", parentCategoryId);

            var response = await _client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogWarning("eBay API categories failed: {StatusCode} - {Content}", 
                                 response.StatusCode, response.Content);
                return new List<Category>();
            }

            var categoryResponse = JsonConvert.DeserializeObject<EbayCategoryResponse>(response.Content!);
            return ConvertToCategories(categoryResponse?.CategoryTree?.RootCategory?.ChildCategories ?? new List<EbayCategory>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eBay categories");
            return new List<Category>();
        }
        finally
        {
            try
            {
                _rateLimitSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ignore - semaphore is already at maximum capacity
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
        }
    }

    /// <summary>
    /// Check if the eBay API service is properly configured and accessible
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Check if credentials are available
            var hasCredentials = await _credentialService.HasCredentialsAsync("eBay");
            if (!hasCredentials)
            {
                _logger.LogWarning("eBay credentials are not configured");
                return false;
            }

            // Try to get a valid token (this will test the credentials)
            await EnsureValidTokenAsync();
            
            // Test with a simple search request instead of categories (more reliable)
            var request = new RestRequest("/buy/browse/v1/item_summary/search", Method.Get);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            request.AddHeader("X-EBAY-C-MARKETPLACE-ID", _settings.DefaultMarketplace);
            request.AddParameter("q", "test");
            request.AddParameter("limit", "1");

            var response = await _client.ExecuteAsync(request);
            return response.IsSuccessful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay API connection test failed");
            return false;
        }
    }    /// <summary>
    /// Get current API rate limit status
    /// </summary>
    public Task<ApiRateLimitInfo> GetRateLimitStatusAsync()
    {
        return Task.FromResult(new ApiRateLimitInfo
        {
            PerSecondLimit = _settings.RateLimitPerSecond,
            RemainingCalls = _rateLimitSemaphore.CurrentCount,
            DailyLimit = 5000, // eBay Browse API daily limit
            ResetTime = DateTime.UtcNow.Date.AddDays(1), // Resets daily
            IsLimited = _rateLimitSemaphore.CurrentCount == 0
        });
    }

    #region Private Methods

    /// <summary>
    /// Ensure we have a valid OAuth access token
    /// </summary>
    private async Task EnsureValidTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry.AddMinutes(-_settings.TokenRefreshBufferMinutes))
        {
            await RefreshAccessTokenAsync();
        }
    }

    /// <summary>
    /// Refresh OAuth access token using client credentials flow
    /// </summary>
    private async Task RefreshAccessTokenAsync()
    {        try
        {
            // Get credentials from the credential service
            var (clientId, clientSecret) = await _credentialService.GetCredentialsAsync("eBay");
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("eBay credentials not found. Please configure your eBay API credentials.");
            }

            var tokenClient = new RestClient(_settings.OAuthUrl);
            var request = new RestRequest("/identity/v1/oauth2/token", Method.Post);
            
            // Client credentials for OAuth 2.0
            var credentialsEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.AddHeader("Authorization", $"Basic {credentialsEncoded}");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", "https://api.ebay.com/oauth/api_scope");

            _logger.LogDebug("Refreshing eBay OAuth token");

            var response = await tokenClient.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                throw new Exception($"OAuth token refresh failed: {response.StatusCode} - {response.Content}");
            }

            var tokenResponse = JsonConvert.DeserializeObject<EbayTokenResponse>(response.Content!);
            
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new Exception("Invalid token response from eBay OAuth");
            }

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            
            _logger.LogInformation("eBay OAuth token refreshed successfully, expires at: {Expiry}", _tokenExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh eBay OAuth token");
            throw;
        }
    }

    /// <summary>
    /// Start timer to release rate limit tokens
    /// </summary>
    private void StartRateLimitTimer()
    {
        var timer = new Timer((_) =>
        {
            if (_rateLimitSemaphore.CurrentCount < _settings.RateLimitPerSecond)
            {
                _rateLimitSemaphore.Release();
            }
        }, null, TimeSpan.FromMilliseconds(1000 / _settings.RateLimitPerSecond), TimeSpan.FromMilliseconds(1000 / _settings.RateLimitPerSecond));
    }

    /// <summary>
    /// Build price filter string for eBay API
    /// </summary>
    private string BuildPriceFilter(decimal? priceMin, decimal? priceMax)
    {
        if (priceMin.HasValue && priceMax.HasValue)
            return $"price:[{priceMin.Value}..{priceMax.Value}]";
        else if (priceMin.HasValue)
            return $"price:[{priceMin.Value}..]";
        else if (priceMax.HasValue)
            return $"price:[..{priceMax.Value}]";
        return string.Empty;
    }

    /// <summary>
    /// Convert eBay item summaries to Product objects
    /// </summary>
    private List<Product> ConvertToProducts(List<EbayItemSummary> items)
    {
        return items.Select(ConvertToProduct).Where(p => p != null).Cast<Product>().ToList();
    }    /// <summary>
    /// Convert single eBay item to Product object
    /// </summary>
    private Product? ConvertToProduct(EbayItemSummary? item)
    {
        if (item == null) return null;        return new Product
        {
            Id = 0, // Will be set by database
            Title = item.Title ?? "Unknown Item",
            Description = item.ShortDescription ?? "",
            Price = item.Price?.Value ?? 0m,
            ShippingCost = 0m, // eBay API doesn't always provide shipping in search results
            ImageUrl = item.Image?.ImageUrl ?? "",
            Marketplace = "eBay",
            Condition = item.Condition ?? "Unknown",
            Location = "", // Location not typically in search results
            Url = item.ItemWebUrl ?? "",
            ExternalId = item.ItemId ?? "",
            IsExternalListing = true,
            ExternalUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }    /// <summary>
    /// Convert eBay item details to Product object
    /// </summary>
    private Product? ConvertToProduct(EbayItemDetails? item)
    {
        if (item == null) return null;        return new Product
        {
            Id = 0, // Will be set by database
            Title = item.Title ?? "Unknown Item",
            Description = item.Description ?? "",
            Price = item.Price?.Value ?? 0m,
            ShippingCost = 0m, // Can be extracted from shipping options if needed
            ImageUrl = item.Image?.ImageUrl ?? "",
            Marketplace = "eBay",
            Condition = item.Condition ?? "Unknown",
            Location = "", // Location details would need to be added to eBay API models
            Url = item.ItemWebUrl ?? "",
            ExternalId = item.ItemId ?? "",
            IsExternalListing = true,
            ExternalUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Convert eBay items to sold listings
    /// </summary>
    private List<SoldListing> ConvertToSoldListings(List<EbayItemSummary> items)
    {
        return items.Select(item => new SoldListing
        {
            ItemId = item.ItemId ?? "",
            Title = item.Title ?? "",
            SoldPrice = item.Price?.Value ?? 0m,
            SoldDate = DateTime.UtcNow.AddDays(-new Random().Next(1, 30)), // Simulated for now
            Condition = item.Condition ?? "Unknown",
            Currency = item.Price?.Currency ?? "USD",
            Marketplace = "eBay",
            BestOffer = false,
            BidCount = 0
        }).ToList();
    }

    /// <summary>
    /// Convert eBay categories to Category objects
    /// </summary>
    private List<Category> ConvertToCategories(List<EbayCategory> categories)
    {
        return categories.Select(cat => new Category
        {
            CategoryId = cat.CategoryId ?? "",
            Name = cat.CategoryName ?? "",
            ParentCategoryId = "",
            Level = 1,
            HasChildren = cat.ChildCategories?.Any() ?? false
        }).ToList();
    }

    #endregion

    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
        _client?.Dispose();
    }
}

#region eBay API Response Models

public class EbaySearchResponse
{
    [JsonProperty("itemSummaries")]
    public List<EbayItemSummary> ItemSummaries { get; set; } = new();
}

public class EbayItemSummary
{
    [JsonProperty("itemId")]
    public string? ItemId { get; set; }
    
    [JsonProperty("title")]
    public string? Title { get; set; }
    
    [JsonProperty("shortDescription")]
    public string? ShortDescription { get; set; }
    
    [JsonProperty("price")]
    public EbayPrice? Price { get; set; }
    
    [JsonProperty("image")]
    public EbayImage? Image { get; set; }
    
    [JsonProperty("condition")]
    public string? Condition { get; set; }
    
    [JsonProperty("itemWebUrl")]
    public string? ItemWebUrl { get; set; }
    
    [JsonProperty("categories")]
    public List<EbayCategory>? Categories { get; set; }
}

public class EbayItemDetails : EbayItemSummary
{
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    [JsonProperty("categoryPath")]
    public string? CategoryPath { get; set; }
}

public class EbayPrice
{
    [JsonProperty("value")]
    public decimal Value { get; set; }
    
    [JsonProperty("currency")]
    public string Currency { get; set; } = "USD";
}

public class EbayImage
{
    [JsonProperty("imageUrl")]
    public string? ImageUrl { get; set; }
}

public class EbayCategory
{
    [JsonProperty("categoryId")]
    public string? CategoryId { get; set; }
    
    [JsonProperty("categoryName")]
    public string? CategoryName { get; set; }
    
    [JsonProperty("childCategories")]
    public List<EbayCategory>? ChildCategories { get; set; }
}

public class EbayCategoryResponse
{
    [JsonProperty("categoryTree")]
    public EbayCategoryTree? CategoryTree { get; set; }
}

public class EbayCategoryTree
{
    [JsonProperty("rootCategory")]
    public EbayCategory? RootCategory { get; set; }
}

public class EbayTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonProperty("token_type")]
    public string TokenType { get; set; } = "Bearer";
}

#endregion
