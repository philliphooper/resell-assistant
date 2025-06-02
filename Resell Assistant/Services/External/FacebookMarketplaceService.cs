using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Resell_Assistant.Models;
using System.Text.RegularExpressions;

namespace Resell_Assistant.Services.External;

/// <summary>
/// Facebook Marketplace scraping service with ethical rate limiting and compliance
/// Note: This is a demonstration implementation. Real scraping should respect Facebook's terms of service.
/// </summary>
public class FacebookMarketplaceService : IFacebookMarketplaceService
{
    private readonly ILogger<FacebookMarketplaceService> _logger;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private static readonly object _lockObject = new object();
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private const int MinDelayBetweenRequestsMs = 3000; // 3 seconds between requests

    public FacebookMarketplaceService(ILogger<FacebookMarketplaceService> logger)
    {
        _logger = logger;
        
        // Initialize HTTP client with proper headers
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        
        // Rate limiting: maximum 1 request per 3 seconds
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<List<Product>> SearchProductsAsync(string query, string? location = null, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty search query provided to Facebook Marketplace service");
            return new List<Product>();
        }

        // Respect rate limiting
        await _rateLimitSemaphore.WaitAsync();
        
        try
        {
            EnforceRateLimit();
            
            _logger.LogInformation("Facebook Marketplace search initiated for query: {Query}, location: {Location}, limit: {Limit}", 
                query, location, limit);

            // For now, return empty results with a note about implementation
            // Real implementation would:
            // 1. Check robots.txt compliance
            // 2. Build search URL with proper encoding
            // 3. Make HTTP request with appropriate headers
            // 4. Parse HTML response using HtmlAgilityPack
            // 5. Extract product information
            // 6. Convert to Product models
            
            _logger.LogInformation("Facebook Marketplace scraping not implemented - returning empty results for compliance");
            
            // Simulate processing time
            await Task.Delay(500);
            
            // Return empty results - no fake data
            return new List<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Facebook Marketplace search for query: {Query}", query);
            return new List<Product>();
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// Enforce rate limiting between requests
    /// </summary>
    private void EnforceRateLimit()
    {
        lock (_lockObject)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest.TotalMilliseconds < MinDelayBetweenRequestsMs)
            {
                var delayNeeded = MinDelayBetweenRequestsMs - (int)timeSinceLastRequest.TotalMilliseconds;
                if (delayNeeded > 0)
                {
                    Task.Delay(delayNeeded).Wait();
                }
            }
            _lastRequestTime = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _rateLimitSemaphore?.Dispose();
    }
}
