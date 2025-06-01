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
        private readonly ILogger<MarketplaceService> _logger;

        public MarketplaceService(
            ApplicationDbContext context,
            IEbayApiService ebayApiService,
            IFacebookMarketplaceService facebookMarketplaceService,
            ILogger<MarketplaceService> logger)
        {
            _context = context;
            _ebayApiService = ebayApiService;
            _facebookMarketplaceService = facebookMarketplaceService;
            _logger = logger;
        }        public async Task<List<Product>> SearchProductsAsync(string query, string? marketplace = null)
        {
            return await SearchProductsAsync(query, marketplace, 20, 15);
        }

        /// <summary>
        /// Search products with configurable limits for external APIs (optimized for dashboard)
        /// </summary>
        public async Task<List<Product>> SearchProductsAsync(string query, string? marketplace, int ebayLimit, int facebookLimit)
        {
            var allProducts = new List<Product>();
            
            // Search local database (existing functionality)
            var localProducts = await SearchLocalProductsAsync(query, marketplace);
            allProducts.AddRange(localProducts);            // Search external APIs based on marketplace filter
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
                }
            }
            
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
            
            // Return combined results, prioritizing newer listings
            var combinedResults = allProducts
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToList();
                
            _logger.LogInformation("Combined search results: {TotalCount} products for query: {Query}", 
                combinedResults.Count, query);
                
            return combinedResults;
        }

        /// <summary>
        /// Search only local database products
        /// </summary>
        private async Task<List<Product>> SearchLocalProductsAsync(string query, string? marketplace)
        {
            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.Title.Contains(query) || 
                                                        (p.Description != null && p.Description.Contains(query)));
            }

            if (!string.IsNullOrEmpty(marketplace))
            {
                productsQuery = productsQuery.Where(p => p.Marketplace == marketplace);
            }

            // Only return local products (not external listings from previous searches)
            return await productsQuery
                .Where(p => !p.IsExternalListing)
                .OrderByDescending(p => p.CreatedAt)
                .Take(25) // Limit local results to make room for external results
                .ToListAsync();
        }

        public async Task<List<Deal>> FindDealsAsync()
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

        public async Task<List<Product>> GetRecentProductsAsync(int count = 10)
        {
            return await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
