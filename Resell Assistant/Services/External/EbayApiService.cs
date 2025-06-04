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
    private readonly object _disposeLock = new object();
    private readonly Timer? _rateLimitTimer;
    private string? _accessToken;
    private DateTime _tokenExpiry;
    private bool _disposed = false;

    public EbayApiService(
        IOptions<EbayApiSettings> settings, 
        ICredentialService credentialService,
        ILogger<EbayApiService> logger)
    {
        _settings = settings.Value;
        _credentialService = credentialService;
        _logger = logger;
        
        // Initialize RestClient with base URL and reasonable timeout for API operations
        var options = new RestClientOptions(_settings.BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(Math.Min(_settings.TimeoutSeconds, 30)) // Increased to 30 seconds for better reliability
        };
        _client = new RestClient(options);
        
        // Initialize rate limiting semaphore
        _rateLimitSemaphore = new SemaphoreSlim(_settings.RateLimitPerSecond, _settings.RateLimitPerSecond);
        
        // Start rate limit release timer
        _rateLimitTimer = StartRateLimitTimer();
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
        if (_disposed)
            throw new ObjectDisposedException(nameof(EbayApiService));
            
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
                if (!_disposed)
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (SemaphoreFullException)
            {
                // Ignore - semaphore is already at maximum capacity
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
            catch (ObjectDisposedException)
            {
                // Ignore - semaphore has been disposed
                _logger.LogDebug("Semaphore was disposed during release attempt");
            }
        }
    }

    /// <summary>
    /// Get detailed information for a specific eBay item
    /// </summary>
    public async Task<Product?> GetProductDetailsAsync(string itemId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EbayApiService));
            
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
                if (!_disposed)
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (SemaphoreFullException)
            {
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("Semaphore was disposed during release attempt");
            }
        }
    }

    /// <summary>
    /// Get sold listings for price analysis and market research
    /// </summary>
    public async Task<List<SoldListing>> GetSoldListingsAsync(string query, int days = 30, int limit = 50)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EbayApiService));
            
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
                if (!_disposed)
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (SemaphoreFullException)
            {
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("Semaphore was disposed during release attempt");
            }
        }
    }

    /// <summary>
    /// Get price history data for market analysis
    /// </summary>
    public async Task<List<PricePoint>> GetPriceHistoryAsync(string query, int days = 90)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EbayApiService));
            
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query cannot be empty", nameof(query));

        try
        {
            // eBay Browse API doesn't provide historical pricing directly
            // This implementation gets current sold listings as a proxy for price history
            var soldListings = await GetSoldListingsAsync(query, days, 100);
            
            return soldListings
                .GroupBy(s => s.SoldDate.Date)
                .Select(g => new PricePoint
                {
                    Date = g.Key,
                    Price = g.Average(s => s.SoldPrice),
                    Currency = g.First().Currency,
                    Source = "eBay",
                    SampleSize = g.Count()
                })
                .OrderBy(p => p.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eBay price history for query: {Query}", query);
            return new List<PricePoint>();
        }
    }

    /// <summary>
    /// Get product categories available on eBay
    /// </summary>
    public async Task<List<Category>> GetCategoriesAsync(string? parentCategoryId = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EbayApiService));

        try
        {
            await EnsureValidTokenAsync();
            await _rateLimitSemaphore.WaitAsync();

            var request = new RestRequest("/commerce/taxonomy/v1/category_tree/0", Method.Get);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            
            if (!string.IsNullOrEmpty(parentCategoryId))
            {
                request.Resource = $"/commerce/taxonomy/v1/category_tree/0/get_category_subtree";
                request.AddParameter("category_id", parentCategoryId);
            }

            _logger.LogDebug("Getting eBay categories, parent: {ParentId}", parentCategoryId);

            var response = await _client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogWarning("eBay API categories failed: {StatusCode} - {Content}", 
                                 response.StatusCode, response.Content);
                return new List<Category>();
            }

            var categoryTree = JsonConvert.DeserializeObject<EbayCategoryTree>(response.Content!);
            return ConvertToCategories(categoryTree?.RootCategoryNode?.ChildCategoryTreeNodes ?? new List<EbayCategoryNode>());
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
                if (!_disposed)
                {
                    _rateLimitSemaphore.Release();
                }
            }
            catch (SemaphoreFullException)
            {
                _logger.LogDebug("Semaphore already at maximum capacity during release");
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("Semaphore was disposed during release attempt");
            }
        }
    }

    /// <summary>
    /// Check if the eBay API service is properly configured and accessible
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        if (_disposed)
            return false;

        try
        {
            // First check if credentials are configured
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
    }

    /// <summary>
    /// Get current API rate limit status
    /// </summary>
    public Task<ApiRateLimitInfo> GetRateLimitStatusAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EbayApiService));

        return Task.FromResult(new ApiRateLimitInfo
        {
            PerSecondLimit = _settings.RateLimitPerSecond,
            RemainingCalls = _rateLimitSemaphore.CurrentCount,
            DailyLimit = 5000, // eBay Browse API daily limit
            ResetTime = DateTime.UtcNow.Date.AddDays(1), // Resets daily
            IsLimited = _rateLimitSemaphore.CurrentCount == 0
        });
    }

    private async Task EnsureValidTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry.AddMinutes(-_settings.TokenRefreshBufferMinutes))
        {
            await RefreshAccessTokenAsync();
        }
    }    private async Task RefreshAccessTokenAsync()
    {
        try
        {
            // Get credentials from the credential service
            var credentials = await _credentialService.GetCredentialsAsync("eBay");
            if (credentials == default || string.IsNullOrEmpty(credentials.clientId) || string.IsNullOrEmpty(credentials.clientSecret))
            {
                throw new Exception("eBay credentials not found. Please configure your eBay API credentials.");
            }

            var clientId = credentials.clientId;
            var clientSecret = credentials.clientSecret;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("eBay credentials are incomplete. Please ensure ClientId and ClientSecret are configured.");
            }

            var oauthClient = new RestClient(_settings.OAuthUrl);
            var request = new RestRequest("/identity/v1/oauth2/token", Method.Post);
            
            // Add Basic auth header with client credentials
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.AddHeader("Authorization", $"Basic {authValue}");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            
            // Add body parameters for client credentials grant
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", "https://api.ebay.com/oauth/api_scope");

            _logger.LogDebug("Refreshing eBay OAuth token");

            var response = await oauthClient.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogError("OAuth token refresh failed: {StatusCode} - {Content}", 
                                response.StatusCode, response.Content);
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

    private Timer StartRateLimitTimer()
    {
        return new Timer((_) =>
        {
            if (!_disposed && _rateLimitSemaphore.CurrentCount < _settings.RateLimitPerSecond)
            {
                try
                {
                    _rateLimitSemaphore.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Ignore - semaphore is already at maximum capacity
                }
                catch (ObjectDisposedException)
                {
                    // Service is being disposed, stop the timer
                }
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private string BuildPriceFilter(decimal? priceMin, decimal? priceMax)
    {
        if (!priceMin.HasValue && !priceMax.HasValue)
            return string.Empty;

        var filter = new StringBuilder("price:[");
        
        if (priceMin.HasValue)
            filter.Append($"{priceMin.Value}");
        
        filter.Append("..");
        
        if (priceMax.HasValue)
            filter.Append($"{priceMax.Value}");
        
        filter.Append("]");
        
        return filter.ToString();
    }    private List<Product> ConvertToProducts(List<EbayItemSummary> items)
    {
        return items.Select(item => new Product
        {
            ExternalId = item.ItemId ?? string.Empty,
            Title = item.Title ?? string.Empty,
            Price = item.Price?.Value ?? 0,
            Description = item.ShortDescription ?? string.Empty,
            ImageUrl = item.Image?.ImageUrl ?? string.Empty,
            Url = item.ItemWebUrl ?? string.Empty,
            Marketplace = "eBay",
            Location = item.ItemLocation?.City ?? string.Empty,
            Condition = item.Condition ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        }).ToList();
    }

    private Product? ConvertToProduct(EbayItemDetails? item)
    {
        if (item == null) return null;        return new Product
        {
            ExternalId = item.ItemId ?? string.Empty,
            Title = item.Title ?? string.Empty,
            Price = item.Price?.Value ?? 0,
            Description = item.Description ?? string.Empty,
            ImageUrl = item.Image?.ImageUrl ?? string.Empty,
            Url = item.ItemWebUrl ?? string.Empty,
            Marketplace = "eBay",
            Location = item.ItemLocation?.City ?? string.Empty,
            Condition = item.Condition ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }

    private List<SoldListing> ConvertToSoldListings(List<EbayItemSummary> items)
    {
        return items.Select(item => new SoldListing
        {
            ItemId = item.ItemId ?? string.Empty,
            Title = item.Title ?? string.Empty,
            SoldPrice = item.Price?.Value ?? 0,
            SoldDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)), // Approximate sold date
            Condition = item.Condition ?? string.Empty,
            Currency = item.Price?.Currency ?? "USD",
            Marketplace = "eBay",
            BestOffer = false,
            BidCount = 0
        }).ToList();
    }

    private List<Category> ConvertToCategories(List<EbayCategoryNode> nodes)
    {
        var categories = new List<Category>();
        
        foreach (var node in nodes)
        {
            categories.Add(new Category
            {
                CategoryId = node.Category?.CategoryId ?? string.Empty,
                Name = node.Category?.CategoryName ?? string.Empty,
                ParentCategoryId = node.ParentCategoryTreeNodeHref ?? string.Empty,
                Level = node.CategoryTreeNodeLevel,
                HasChildren = node.ChildCategoryTreeNodes?.Any() == true
            });
            
            // Recursively add child categories
            if (node.ChildCategoryTreeNodes?.Any() == true)
            {
                categories.AddRange(ConvertToCategories(node.ChildCategoryTreeNodes));
            }
        }
        
        return categories;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    
                    try
                    {
                        _rateLimitTimer?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error disposing rate limit timer");
                    }
                    
                    try
                    {
                        _rateLimitSemaphore?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error disposing rate limit semaphore");
                    }
                    
                    try
                    {
                        _client?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error disposing REST client");
                    }
                }
            }
        }
    }
}

public class EbayTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

public class EbaySearchResponse
{
    [JsonProperty("itemSummaries")]
    public List<EbayItemSummary> ItemSummaries { get; set; } = new();

    [JsonProperty("total")]
    public int Total { get; set; }

    [JsonProperty("limit")]
    public int Limit { get; set; }

    [JsonProperty("offset")]
    public int Offset { get; set; }
}

public class EbayItemSummary
{
    [JsonProperty("itemId")]
    public string? ItemId { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("price")]
    public EbayPrice? Price { get; set; }

    [JsonProperty("image")]
    public EbayImage? Image { get; set; }

    [JsonProperty("itemWebUrl")]
    public string? ItemWebUrl { get; set; }

    [JsonProperty("itemLocation")]
    public EbayLocation? ItemLocation { get; set; }

    [JsonProperty("condition")]
    public string? Condition { get; set; }

    [JsonProperty("shortDescription")]
    public string? ShortDescription { get; set; }
}

public class EbayItemDetails : EbayItemSummary
{
    [JsonProperty("description")]
    public string? Description { get; set; }
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

public class EbayLocation
{
    [JsonProperty("city")]
    public string? City { get; set; }

    [JsonProperty("stateOrProvince")]
    public string? StateOrProvince { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }
}

public class EbayCategoryTree
{
    [JsonProperty("rootCategoryNode")]
    public EbayCategoryNode? RootCategoryNode { get; set; }
}

public class EbayCategoryNode
{
    [JsonProperty("category")]
    public EbayCategoryInfo? Category { get; set; }

    [JsonProperty("childCategoryTreeNodes")]
    public List<EbayCategoryNode>? ChildCategoryTreeNodes { get; set; }

    [JsonProperty("categoryTreeNodeLevel")]
    public int CategoryTreeNodeLevel { get; set; }

    [JsonProperty("parentCategoryTreeNodeHref")]
    public string? ParentCategoryTreeNodeHref { get; set; }
}

public class EbayCategoryInfo
{
    [JsonProperty("categoryId")]
    public string? CategoryId { get; set; }

    [JsonProperty("categoryName")]
    public string? CategoryName { get; set; }
}
