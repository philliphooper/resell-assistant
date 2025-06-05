using Microsoft.AspNetCore.Mvc;
using Resell_Assistant.Services;
using Resell_Assistant.Models;
using Resell_Assistant.DTOs;

namespace Resell_Assistant.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DealsController : ControllerBase
    {
        private readonly IDealDiscoveryService _dealDiscoveryService;
        private readonly IMarketplaceService _marketplaceService;
        private readonly IPriceAnalysisService _priceAnalysisService;
        private readonly ILogger<DealsController> _logger;

        public DealsController(
            IDealDiscoveryService dealDiscoveryService,
            IMarketplaceService marketplaceService,
            IPriceAnalysisService priceAnalysisService,
            ILogger<DealsController> logger)
        {
            _dealDiscoveryService = dealDiscoveryService;
            _marketplaceService = marketplaceService;
            _priceAnalysisService = priceAnalysisService;
            _logger = logger;
        }

        /// <summary>
        /// Get top deals from the database
        /// </summary>
        [HttpGet("top")]
        public async Task<ActionResult<List<Deal>>> GetTopDeals([FromQuery] int limit = 20)
        {
            try
            {
                if (limit <= 0 || limit > 100)
                {
                    return BadRequest("Limit must be between 1 and 100");
                }

                var topDeals = await _marketplaceService.FindDealsAsync();
                return Ok(topDeals.Take(limit));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top deals");
                return StatusCode(500, "Error retrieving top deals");
            }
        }        /// <summary>
        /// Discover new cross-marketplace deals in real-time
        /// </summary>
        [HttpGet("discover")]
        public async Task<ActionResult<List<Deal>>> DiscoverDeals([FromQuery] int maxResults = 20)
        {
            try
            {
                _logger.LogInformation("Discovering deals with maxResults: {MaxResults}", maxResults);
                
                var deals = await _dealDiscoveryService.DiscoverCrossMarketplaceDealsAsync(maxResults);
                
                _logger.LogInformation("Discovered {Count} deals", deals.Count);
                return Ok(deals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering deals");
                return StatusCode(500, "Error discovering deals: " + ex.Message);
            }
        }

        /// <summary>
        /// Find price discrepancies for a specific search term
        /// </summary>
        [HttpGet("price-discrepancies")]
        public async Task<ActionResult<List<Deal>>> FindPriceDiscrepancies(
            [FromQuery] string query,
            [FromQuery] int minProfitMargin = 15)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query parameter is required");
                }

                if (minProfitMargin < 0 || minProfitMargin > 100)
                {
                    return BadRequest("Minimum profit margin must be between 0 and 100");
                }

                _logger.LogInformation("Finding price discrepancies for query: {Query}, min margin: {MinMargin}%", 
                    query, minProfitMargin);

                var deals = await _dealDiscoveryService.FindPriceDiscrepanciesAsync(query, minProfitMargin);
                
                _logger.LogInformation("Found {Count} price discrepancy deals for query: {Query}", deals.Count, query);
                return Ok(deals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding price discrepancies for query: {Query}", query);
                return StatusCode(500, "Error finding price discrepancies");
            }
        }

        /// <summary>
        /// Scan for real-time deals from recently listed products
        /// </summary>
        [HttpGet("real-time")]
        public async Task<ActionResult<List<Deal>>> GetRealTimeDeals()
        {
            try
            {
                _logger.LogInformation("Scanning for real-time deals");
                
                var deals = await _dealDiscoveryService.ScanForRealTimeDealsAsync();
                
                _logger.LogInformation("Found {Count} real-time deals", deals.Count);
                return Ok(deals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for real-time deals");
                return StatusCode(500, "Error scanning for real-time deals");
            }
        }

        /// <summary>
        /// Analyze a specific product for deal potential
        /// </summary>
        [HttpPost("analyze/{productId}")]
        public async Task<ActionResult<Deal>> AnalyzeProductForDeal(int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return BadRequest("Product ID must be a positive number");
                }

                var product = await _marketplaceService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return NotFound("Product not found");
                }

                var deal = await _dealDiscoveryService.AnalyzePotentialDealAsync(product);
                if (deal == null)
                {
                    return Ok(new { message = "No significant deal opportunity found for this product" });
                }

                return Ok(deal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing product {ProductId} for deal potential", productId);
                return StatusCode(500, "Error analyzing product for deal potential");
            }
        }

        /// <summary>
        /// Get deals filtered by criteria
        /// </summary>
        [HttpGet("filtered")]
        public async Task<ActionResult<List<Deal>>> GetFilteredDeals(
            [FromQuery] int? minScore,
            [FromQuery] decimal? minProfit,
            [FromQuery] string? marketplace,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (limit <= 0 || limit > 100)
                {
                    return BadRequest("Limit must be between 1 and 100");
                }

                var allDeals = await _marketplaceService.FindDealsAsync();
                var filteredDeals = allDeals.AsQueryable();

                if (minScore.HasValue)
                {
                    filteredDeals = filteredDeals.Where(d => d.DealScore >= minScore.Value);
                }

                if (minProfit.HasValue)
                {
                    filteredDeals = filteredDeals.Where(d => d.PotentialProfit >= minProfit.Value);
                }

                if (!string.IsNullOrWhiteSpace(marketplace))
                {
                    filteredDeals = filteredDeals.Where(d => d.Product != null && 
                        d.Product.Marketplace.Equals(marketplace, StringComparison.OrdinalIgnoreCase));
                }

                var result = filteredDeals
                    .OrderByDescending(d => d.DealScore)
                    .Take(limit)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered deals");
                return StatusCode(500, "Error retrieving filtered deals");
            }
        }

        /// <summary>
        /// Get deal statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDealStats()
        {
            try
            {
                var allDeals = await _marketplaceService.FindDealsAsync();

                var stats = new
                {
                    TotalDeals = allDeals.Count,                    AverageScore = allDeals.Any() ? (int)allDeals.Average(d => d.DealScore) : 0,
                    TotalPotentialProfit = allDeals.Sum(d => d.PotentialProfit),
                    HighValueDeals = allDeals.Count(d => d.DealScore >= 80),
                    MarketplaceBreakdown = allDeals
                        .Where(d => d.Product?.Marketplace != null)
                        .GroupBy(d => d.Product!.Marketplace)
                        .Select(g => new { Marketplace = g.Key, Count = g.Count() })
                        .ToList(),
                    RecentDeals = allDeals
                        .Where(d => d.CreatedAt > DateTime.UtcNow.AddDays(-7))
                        .Count()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deal statistics");
                return StatusCode(500, "Error retrieving deal statistics");
            }
        }

        /// <summary>
        /// Intelligent deal discovery with exact result counts and live progress
        /// </summary>
        [HttpPost("intelligent-discovery")]
        public async Task<ActionResult<List<Deal>>> DiscoverIntelligentDeals([FromBody] DealDiscoverySettingsDto settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Starting intelligent discovery with exact result count: {Count}", settings.ExactResultCount);

                // Validate that we can potentially find the requested number of deals
                var canFulfill = await _dealDiscoveryService.ValidateExactResultCountAsync(settings.ExactResultCount);
                if (!canFulfill)
                {
                    return BadRequest($"Cannot guarantee {settings.ExactResultCount} deals with current data. Try a lower number or wait for more products to be indexed.");
                }

                var deals = await _dealDiscoveryService.DiscoverIntelligentDealsAsync(settings);
                
                _logger.LogInformation("Intelligent discovery completed: {ActualCount} deals found", deals.Count);
                
                return Ok(deals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during intelligent deal discovery");
                return StatusCode(500, $"Error during intelligent discovery: {ex.Message}");
            }
        }

        /// <summary>
        /// Intelligent deal discovery with live progress updates via Server-Sent Events
        /// </summary>
        [HttpPost("intelligent-discovery-stream")]
        public async Task DiscoverIntelligentDealsWithProgress([FromBody] DealDiscoverySettingsDto settings)
        {
            try
            {                if (!ModelState.IsValid)
                {
                    Response.StatusCode = 400;
                    await Response.WriteAsync("Invalid settings provided");
                    return;
                }

                // Start the response early to prevent TempData conflicts with SSE
                Response.Headers["Content-Type"] = "text/event-stream";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["Connection"] = "keep-alive";
                Response.Headers["Access-Control-Allow-Origin"] = "*";
                
                await Response.StartAsync(); // This prevents TempData from interfering
                var progress = new Progress<DiscoveryProgressDto>(async progressUpdate =>
                {
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(progressUpdate);
                        await Response.WriteAsync($"data: {json}\n\n");
                        await Response.Body.FlushAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Client disconnected, ignore
                    }
                    catch (InvalidOperationException)
                    {
                        // Response already completed, ignore
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send progress update");
                    }
                });

                _logger.LogInformation("Starting intelligent discovery stream with exact result count: {Count}", settings.ExactResultCount);

                var deals = await _dealDiscoveryService.DiscoverIntelligentDealsAsync(settings, progress);
                
                // Send final results
                var finalData = new { 
                    type = "complete", 
                    deals = deals,
                    totalCount = deals.Count
                };
                var finalJson = System.Text.Json.JsonSerializer.Serialize(finalData);
                await Response.WriteAsync($"data: {finalJson}\n\n");
                await Response.Body.FlushAsync();

                _logger.LogInformation("Intelligent discovery stream completed: {ActualCount} deals found", deals.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during intelligent deal discovery stream");
                
                var errorData = new { 
                    type = "error", 
                    message = ex.Message
                };
                var errorJson = System.Text.Json.JsonSerializer.Serialize(errorData);
                await Response.WriteAsync($"data: {errorJson}\n\n");
                await Response.Body.FlushAsync();
            }
        }

        /// <summary>
        /// Get comparison listings for a specific deal (transparency feature)
        /// </summary>
        [HttpGet("{dealId}/comparison-listings")]
        public async Task<ActionResult<List<ComparisonListingDto>>> GetComparisonListings(int dealId)
        {
            try
            {
                if (dealId <= 0)
                {
                    return BadRequest("Deal ID must be a positive number");
                }

                // Get deal with comparison listings from database
                var deal = await _marketplaceService.GetDealWithComparisonListingsAsync(dealId);
                if (deal == null)
                {
                    return NotFound("Deal not found");
                }

                var comparisonListings = deal.ComparisonListings.Select(cl => new ComparisonListingDto
                {
                    ProductId = cl.ProductId,
                    Title = cl.Title,
                    Price = cl.Price,
                    ShippingCost = cl.ShippingCost,
                    Marketplace = cl.Marketplace,
                    Condition = cl.Condition,
                    Location = cl.Location,
                    Url = cl.Url,
                    DateListed = cl.DateListed,
                    IsSelectedDeal = cl.IsSelectedDeal
                }).ToList();

                return Ok(comparisonListings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comparison listings for deal {DealId}", dealId);
                return StatusCode(500, "Error retrieving comparison listings");
            }
        }

        /// <summary>
        /// Get trending products for deal discovery
        /// </summary>
        [HttpGet("trending-products")]
        public async Task<ActionResult<List<Product>>> GetTrendingProducts([FromQuery] int count = 10, [FromQuery] string? searchTerms = null)
        {
            try
            {
                if (count <= 0 || count > 50)
                {
                    return BadRequest("Count must be between 1 and 50");
                }

                var trendingProducts = await _dealDiscoveryService.FindTrendingProductsAsync(count, searchTerms);
                
                return Ok(trendingProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending products");
                return StatusCode(500, "Error retrieving trending products");
            }
        }

        /// <summary>
        /// Validate if exact result count is achievable
        /// </summary>
        [HttpGet("validate-count/{count}")]
        public async Task<ActionResult<object>> ValidateExactResultCount(int count)
        {
            try
            {
                if (count <= 0 || count > 100)
                {
                    return BadRequest("Count must be between 1 and 100");
                }

                var canFulfill = await _dealDiscoveryService.ValidateExactResultCountAsync(count);
                
                return Ok(new { 
                    canFulfill = canFulfill,
                    requestedCount = count,
                    message = canFulfill 
                        ? $"Can potentially find {count} deals"
                        : $"May not be able to find {count} deals with current data"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exact result count");
                return StatusCode(500, "Error validating result count");
            }
        }
    }
}
