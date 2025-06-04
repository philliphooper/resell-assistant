using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;
using Resell_Assistant.Services.External;

namespace Resell_Assistant.Services
{
    public class MarketplaceService : IMarketplaceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEbayApiService _ebayApiService;
        private readonly IFacebookMarketplaceService _facebookMarketplaceService;
        private readonly ILogger<MarketplaceService> _logger;        public MarketplaceService(
            ApplicationDbContext context,
            IEbayApiService ebayApiService,
            ILogger<MarketplaceService> logger)
        {
            _context = context;
            _ebayApiService = ebayApiService;
            _facebookMarketplaceService = null!; // Temporarily disabled
            _logger = logger;
        }public async Task<List<Product>> SearchProductsAsync(string query, string? marketplace = null)
        {
            return await SearchProductsAsync(query, marketplace, 20, 15);
        }        /// <summary>
        /// Search products with configurable limits for external APIs (optimized for dashboard)
        /// </summary>
        public async Task<List<Product>> SearchProductsAsync(string query, string? marketplace, int ebayLimit, int facebookLimit)
        {
            var allProducts = new List<Product>();
            
            // ONLY search external APIs - no database queries for product discovery
            
            // Search external APIs based on marketplace filter
            if (string.IsNullOrEmpty(marketplace) || marketplace.Equals("eBay", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogInformation("Searching eBay for query: {Query} (limit: {Limit})", query, ebayLimit);
                    var ebayProducts = await _ebayApiService.SearchProductsAsync(query, limit: ebayLimit);
                    allProducts.AddRange(ebayProducts);
                    _logger.LogInformation("Found {Count} eBay products for query: {Query}", ebayProducts.Count, query);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "eBay search failed for query: {Query}", query);
                }            }
              
            // Facebook Marketplace temporarily disabled
            // TODO: Re-enable when proper implementation is complete
            /*
            if (string.IsNullOrEmpty(marketplace) || marketplace.Equals("Facebook Marketplace", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogInformation("Searching Facebook Marketplace for query: {Query} (limit: {Limit})", query, facebookLimit);
                    var facebookProducts = await _facebookMarketplaceService.SearchProductsAsync(query, limit: facebookLimit);
                    allProducts.AddRange(facebookProducts);
                    _logger.LogInformation("Found {Count} Facebook Marketplace products for query: {Query}", facebookProducts.Count, query);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Facebook Marketplace search failed for query: {Query}", query);
                }
            }
            */
            
            // Return combined results, prioritizing newer listings
            var combinedResults = allProducts
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToList();
                
            _logger.LogInformation("Combined search results: {TotalCount} products for query: {Query}", 
                combinedResults.Count, query);
                
            return combinedResults;
        }        public async Task<List<Deal>> FindDealsAsync()
        {
            return await _context.Deals
                .Include(d => d.Product)
                .OrderByDescending(d => d.DealScore)
                .Take(20)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        // This method should only be used for getting user's saved/favorited products, not for discovery
        public async Task<List<Product>> GetRecentProductsAsync(int count = 10)
        {
            // Only return user's saved products, not for deal discovery
            return await _context.Products
                .Where(p => !p.IsExternalListing) // Only user's own products
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Deal?> GetDealWithComparisonListingsAsync(int dealId)
        {
            return await _context.Deals
                .Include(d => d.Product)
                .Include(d => d.ComparisonListings)
                .FirstOrDefaultAsync(d => d.Id == dealId);
        }
    }
}
