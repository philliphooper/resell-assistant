using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;
using Resell_Assistant.Services;

namespace Resell_Assistant.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarketplaceService _marketplaceService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context, 
            IMarketplaceService marketplaceService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _marketplaceService = marketplaceService;
            _logger = logger;
        }        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            try
            {
                _logger.LogInformation("Fetching dashboard stats including live marketplace data");

                // Get counts from local database
                var localProducts = await _context.Products.CountAsync();
                var totalDeals = await _context.Deals.CountAsync();

                // Fetch live marketplace data with timeout and parallel processing
                var liveProducts = new List<Product>();
                var searchQueries = new[] { "iPhone", "Samsung", "PlayStation", "Xbox", "MacBook" };
                
                // Use timeout for external API calls to prevent dashboard timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // 3-second timeout for external calls
                
                // Execute searches in parallel with timeout handling
                var searchTasks = searchQueries.Select(async query =>
                {
                    try
                    {
                        // Use Task.Run with timeout to prevent hanging
                        var searchTask = Task.Run(async () =>
                        {
                            return await _marketplaceService.SearchProductsAsync(query, null, 3, 3); // Reduced limits for faster response
                        });
                        
                        var searchResults = await searchTask.WaitAsync(TimeSpan.FromSeconds(2)); // 2-second timeout per query
                        
                        // Only count external listings to avoid double-counting local products
                        var externalProducts = searchResults.Where(p => p.IsExternalListing).ToList();
                        _logger.LogDebug("Found {Count} external products for query: {Query}", externalProducts.Count, query);
                        return externalProducts;
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("Search timeout for query: {Query} (using fallback data)", query);
                        return new List<Product>();
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Search cancelled for query: {Query} (using fallback data)", query);
                        return new List<Product>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch live data for query: {Query}", query);
                        return new List<Product>();
                    }
                });

                // Execute with semaphore to limit concurrent API calls (prevent rate limiting)
                var semaphore = new SemaphoreSlim(2, 2); // Max 2 concurrent calls
                var throttledTasks = searchTasks.Select(async task =>
                {
                    await semaphore.WaitAsync(cts.Token);
                    try
                    {
                        return await task;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                try
                {
                    var searchResults = await Task.WhenAll(throttledTasks);
                    foreach (var result in searchResults)
                    {
                        liveProducts.AddRange(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Dashboard external API calls timed out after 3 seconds, using available data");
                }

                // Remove duplicates based on external ID
                var uniqueLiveProducts = liveProducts
                    .GroupBy(p => p.ExternalId)
                    .Select(g => g.First())
                    .ToList();

                var totalProducts = localProducts + uniqueLiveProducts.Count;

                _logger.LogInformation("Dashboard stats: {LocalProducts} local + {LiveProducts} live = {TotalProducts} total products", 
                    localProducts, uniqueLiveProducts.Count, totalProducts);

                // Calculate total profit from deals
                var totalProfit = await _context.Deals.SumAsync(d => d.PotentialProfit);

                // Calculate average deal score
                var averageDealScore = totalDeals > 0 
                    ? await _context.Deals.AverageAsync(d => d.DealScore)
                    : 0;

                // Find top marketplace (including live data)
                var marketplaceCounts = new Dictionary<string, int>();
                
                // Count local products by marketplace
                var localMarketplaceCounts = await _context.Products
                    .GroupBy(p => p.Marketplace)
                    .Select(g => new { Marketplace = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var item in localMarketplaceCounts)
                {
                    marketplaceCounts[item.Marketplace] = item.Count;
                }

                // Add live product counts
                var liveMarketplaceCounts = uniqueLiveProducts
                    .GroupBy(p => p.Marketplace)
                    .Select(g => new { Marketplace = g.Key, Count = g.Count() });

                foreach (var item in liveMarketplaceCounts)
                {
                    marketplaceCounts[item.Marketplace] = marketplaceCounts.GetValueOrDefault(item.Marketplace, 0) + item.Count;
                }

                var topMarketplace = marketplaceCounts.Any() 
                    ? marketplaceCounts.OrderByDescending(x => x.Value).First().Key 
                    : "N/A";

                // Count recent deals (last 7 days)
                var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
                var recentDealsCount = await _context.Deals
                    .Where(d => d.CreatedAt > oneWeekAgo)
                    .CountAsync();

                // Calculate weekly profit
                var weeklyProfit = await _context.Deals
                    .Where(d => d.CreatedAt > oneWeekAgo)
                    .SumAsync(d => d.PotentialProfit);

                // Count active alerts
                var activeAlerts = await _context.SearchAlerts
                    .Where(a => a.IsActive)
                    .CountAsync();                // Calculate portfolio value (sum of purchase prices for unsold items)
                var portfolioValue = await _context.UserPortfolios
                    .Where(p => p.Status != "Sold")
                    .SumAsync(p => (decimal?)p.PurchasePrice) ?? 0;

                // Get top categories from both local and live product titles
                var allLocalProducts = await _context.Products.Select(p => p.Title).ToListAsync();
                var allLiveProductTitles = uniqueLiveProducts.Select(p => p.Title).ToList();
                var allProductTitles = allLocalProducts.Concat(allLiveProductTitles).ToList();

                var categoryKeywords = new Dictionary<string, string[]>
                {
                    { "Electronics", new[] { "iPhone", "iPad", "MacBook", "PlayStation", "Switch", "Samsung", "Apple", "Watch", "AirPods" } },
                    { "Gaming", new[] { "PlayStation", "Xbox", "Nintendo", "Switch", "PS5", "Console", "Gaming" } },
                    { "Mobile", new[] { "iPhone", "Samsung", "Galaxy", "Phone", "Mobile" } },
                    { "Computers", new[] { "MacBook", "Laptop", "Computer", "PC", "iMac" } },
                    { "Audio", new[] { "AirPods", "Headphones", "Speakers", "Audio", "Beats" } }
                };

                var categoryCount = new Dictionary<string, int>();
                foreach (var productTitle in allProductTitles)
                {
                    foreach (var category in categoryKeywords)
                    {
                        if (category.Value.Any(keyword => productTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                        {
                            categoryCount[category.Key] = categoryCount.GetValueOrDefault(category.Key, 0) + 1;
                            break; // Only count each product in one category
                        }
                    }
                }

                var topCategories = categoryCount
                    .OrderByDescending(c => c.Value)
                    .Take(5)
                    .Select(c => c.Key)
                    .ToList();;                // Get recent deals with product details
                var recentDeals = await _context.Deals
                    .Include(d => d.Product)
                    .Where(d => d.CreatedAt > oneWeekAgo && d.Product != null)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(10)
                    .Select(d => new
                    {
                        d.Id,
                        d.ProductId,
                        d.PotentialProfit,
                        d.EstimatedSellPrice,
                        d.DealScore,
                        d.Confidence,
                        d.Reasoning,
                        d.CreatedAt,
                        Product = new
                        {
                            d.Product!.Id,
                            d.Product.Title,
                            d.Product.Description,
                            d.Product.Price,
                            d.Product.ShippingCost,
                            d.Product.Marketplace,
                            d.Product.Condition,
                            d.Product.Location,
                            d.Product.Url,
                            d.Product.ImageUrl,
                            d.Product.CreatedAt
                        }
                    })
                    .ToListAsync();

                var stats = new
                {
                    totalProducts,
                    totalDeals,
                    totalProfit = Math.Round(totalProfit, 2),
                    averageDealScore = Math.Round(averageDealScore, 0),
                    topMarketplace,
                    recentDealsCount,
                    activeAlerts,
                    portfolioValue = Math.Round(portfolioValue, 2),
                    weeklyProfit = Math.Round(weeklyProfit, 2),
                    topCategories,
                    recentDeals,
                    // Additional live marketplace metrics
                    liveProductsCount = uniqueLiveProducts.Count,
                    localProductsCount = localProducts,
                    marketplaceCounts = marketplaceCounts.OrderByDescending(x => x.Value).Take(5).ToDictionary(x => x.Key, x => x.Value)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to calculate dashboard stats", error = ex.Message });
            }
        }

        /// <summary>
        /// Test eBay API connectivity and configuration - Issue #15
        /// </summary>
        [HttpGet("test-ebay-connection")]
        public ActionResult TestEbayConnection()
        {
            try
            {
                // Get eBay configuration from appsettings
                var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                
                var clientId = configuration["ApiKeys:ClientId"];
                var clientSecret = configuration["ApiKeys:ClientSecret"];
                var environment = configuration["ApiKeys:Environment"] ?? "production";
                var baseUrl = configuration["ApiKeys:BaseUrl"] ?? "https://api.ebay.com";

                var result = new
                {
                    timestamp = DateTime.UtcNow,
                    environment = environment,
                    baseUrl = baseUrl,
                    clientIdConfigured = !string.IsNullOrEmpty(clientId) && !clientId.Contains("YOUR_"),
                    clientSecretConfigured = !string.IsNullOrEmpty(clientSecret) && !clientSecret.Contains("YOUR_"),
                    configurationStatus = (!string.IsNullOrEmpty(clientId) && !clientId.Contains("YOUR_") && 
                                         !string.IsNullOrEmpty(clientSecret) && !clientSecret.Contains("YOUR_")) 
                                         ? "Ready for API Integration" : "Needs Real API Credentials",
                    nextSteps = (!string.IsNullOrEmpty(clientId) && !clientId.Contains("YOUR_")) 
                               ? "Ready to implement eBay API service (Issue #11)"
                               : "Complete eBay Developer setup (Issue #15)"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Failed to test eBay API configuration", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Simple health check endpoint for API connectivity testing
        /// </summary>
        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                message = "API is operational"
            });
        }
    }
}