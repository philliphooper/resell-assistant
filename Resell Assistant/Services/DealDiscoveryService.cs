using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;
using Resell_Assistant.Services.External;

namespace Resell_Assistant.Services
{    public interface IDealDiscoveryService
    {
        Task<List<Deal>> DiscoverCrossMarketplaceDealsAsync(int maxResults = 20);
        Task<List<Deal>> FindPriceDiscrepanciesAsync(string query, int minProfitMargin = 15, int maxSearchResults = 10);
        Task<Deal?> AnalyzePotentialDealAsync(Product product);
        Task<List<Deal>> ScanForRealTimeDealsAsync();
    }

    public class DealDiscoveryService : IDealDiscoveryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarketplaceService _marketplaceService;
        private readonly IPriceAnalysisService _priceAnalysisService;
        private readonly IEbayApiService _ebayApiService;
        private readonly IFacebookMarketplaceService _facebookMarketplaceService;
        private readonly ILogger<DealDiscoveryService> _logger;

        public DealDiscoveryService(
            ApplicationDbContext context,
            IMarketplaceService marketplaceService,
            IPriceAnalysisService priceAnalysisService,
            IEbayApiService ebayApiService,
            IFacebookMarketplaceService facebookMarketplaceService,
            ILogger<DealDiscoveryService> logger)
        {
            _context = context;
            _marketplaceService = marketplaceService;
            _priceAnalysisService = priceAnalysisService;
            _ebayApiService = ebayApiService;
            _facebookMarketplaceService = facebookMarketplaceService;
            _logger = logger;
        }        public async Task<List<Deal>> DiscoverCrossMarketplaceDealsAsync(int maxResults = 20)
        {
            _logger.LogInformation("Starting cross-marketplace deal discovery");

            var allDeals = new List<Deal>();

            // Get a limited set of trending search terms for faster discovery
            var trendingTerms = GetTrendingSearchTerms().Take(3).ToList(); // Limit to 3 terms for speed

            foreach (var term in trendingTerms)
            {
                try
                {
                    // Limit search results per term to speed up the process
                    var deals = await FindPriceDiscrepanciesAsync(term, 15, 5); // Max 5 results per term
                    allDeals.AddRange(deals);
                    
                    // Break early if we have enough deals
                    if (allDeals.Count >= maxResults * 2) break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to find deals for term: {Term}", term);
                }
            }

            // Skip recent products scan for faster response (comment out for now)
            // var recentDeals = await ScanRecentProductsForDeals();
            // allDeals.AddRange(recentDeals);

            // Return best deals by score
            var topDeals = allDeals
                .GroupBy(d => d.ProductId)
                .Select(g => g.OrderByDescending(d => d.DealScore).First())
                .OrderByDescending(d => d.DealScore)
                .Take(maxResults)
                .ToList();

            _logger.LogInformation("Discovered {Count} cross-marketplace deals", topDeals.Count);
            return topDeals;
        }        public async Task<List<Deal>> FindPriceDiscrepanciesAsync(string query, int minProfitMargin = 15, int maxSearchResults = 10)
        {
            _logger.LogInformation("Finding price discrepancies for query: {Query}", query);

            var deals = new List<Deal>();

            // Search across all marketplaces with limited results for speed
            var ebayProducts = await SearchMarketplaceSafely("eBay", query, maxSearchResults);
            var facebookProducts = await SearchMarketplaceSafely("Facebook Marketplace", query, maxSearchResults);

            _logger.LogInformation("Found {EbayCount} eBay products and {FacebookCount} Facebook products for query: {Query}", 
                ebayProducts.Count, facebookProducts.Count, query);

            // If we don't have products from both marketplaces, we can't find cross-marketplace deals
            if (ebayProducts.Count == 0 && facebookProducts.Count == 0)
            {
                _logger.LogWarning("No products found from any marketplace for query: {Query}", query);
                return deals;
            }

            // If we only have one marketplace, let's still try to find deals by comparing within that marketplace
            if (ebayProducts.Count == 0 || facebookProducts.Count == 0)
            {
                _logger.LogInformation("Only one marketplace has results for query: {Query}. eBay: {EbayCount}, Facebook: {FacebookCount}", 
                    query, ebayProducts.Count, facebookProducts.Count);
                
                // For now, let's create some synthetic cross-marketplace comparison
                // This simulates finding similar products at different prices
                var availableProducts = ebayProducts.Concat(facebookProducts).ToList();
                if (availableProducts.Any())
                {
                    var bestDeals = await CreateSyntheticDealsFromSingleMarketplace(availableProducts, minProfitMargin);
                    deals.AddRange(bestDeals);
                }
            }
            else
            {
                // Group similar products by normalized title
                var productGroups = GroupSimilarProducts(ebayProducts.Concat(facebookProducts).ToList());

                _logger.LogInformation("Created {GroupCount} product groups for comparison", productGroups.Count);

                foreach (var group in productGroups.Where(g => g.Value.Count > 1))
                {
                    var groupDeals = await AnalyzeProductGroup(group.Value, minProfitMargin);
                    deals.AddRange(groupDeals);
                }
            }

            _logger.LogInformation("Found {DealCount} deals for query: {Query}", deals.Count, query);
            return deals.OrderByDescending(d => d.DealScore).ToList();
        }

        public async Task<Deal?> AnalyzePotentialDealAsync(Product product)
        {
            try
            {
                // Get similar products from other marketplaces
                var similarProducts = await FindSimilarProductsAcrossMarketplaces(product);

                if (!similarProducts.Any())
                    return null;

                // Find the highest selling price from similar products
                var maxSellingPrice = similarProducts.Max(p => p.Price);
                var totalCost = product.Price + product.ShippingCost;
                var potentialProfit = maxSellingPrice - totalCost;

                if (potentialProfit <= 0)
                    return null;

                var profitMargin = (potentialProfit / totalCost) * 100;

                // Only consider it a deal if profit margin is significant
                if (profitMargin < 10)
                    return null;

                var deal = await _priceAnalysisService.AnalyzeProductAsync(product);
                deal.EstimatedSellPrice = maxSellingPrice;
                deal.PotentialProfit = potentialProfit;
                deal.Reasoning += $" Found similar items selling for up to ${maxSellingPrice:F2} on other platforms.";

                return deal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze potential deal for product {ProductId}", product.Id);
                return null;
            }
        }

        public async Task<List<Deal>> ScanForRealTimeDealsAsync()
        {
            _logger.LogInformation("Scanning for real-time deals");

            var deals = new List<Deal>();

            // Get recently listed products (last 24 hours)
            var recentProducts = await _context.Products
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddHours(-24))
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();

            foreach (var product in recentProducts)
            {
                var deal = await AnalyzePotentialDealAsync(product);
                if (deal != null && deal.DealScore >= 60)
                {
                    deals.Add(deal);
                }
            }

            return deals.OrderByDescending(d => d.DealScore).ToList();
        }

        private async Task<List<Product>> SearchMarketplaceSafely(string marketplace, string query, int limit)
        {
            try
            {
                return await _marketplaceService.SearchProductsAsync(query, marketplace, limit, limit);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search {Marketplace} for query: {Query}", marketplace, query);
                return new List<Product>();
            }
        }

        private Dictionary<string, List<Product>> GroupSimilarProducts(List<Product> products)
        {
            return products
                .GroupBy(p => NormalizeProductTitle(p.Title))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private string NormalizeProductTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;

            // Remove common variations and normalize for grouping
            return title.ToLower()
                .Replace("new", "")
                .Replace("used", "")
                .Replace("excellent", "")
                .Replace("good", "")
                .Replace("fair", "")
                .Replace("-", " ")
                .Replace("_", " ")
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(4) // First 4 significant words
                .Aggregate((a, b) => $"{a} {b}");
        }

        private async Task<List<Deal>> AnalyzeProductGroup(List<Product> products, int minProfitMargin)
        {
            var deals = new List<Deal>();

            // Find the cheapest and most expensive in the group
            var cheapest = products.OrderBy(p => p.Price + p.ShippingCost).First();
            var mostExpensive = products.OrderByDescending(p => p.Price + p.ShippingCost).First();

            var totalCostCheapest = cheapest.Price + cheapest.ShippingCost;
            var totalCostMostExpensive = mostExpensive.Price + mostExpensive.ShippingCost;
            var potentialProfit = totalCostMostExpensive - totalCostCheapest;

            if (potentialProfit > 0)
            {
                var profitMargin = (potentialProfit / totalCostCheapest) * 100;

                if (profitMargin >= minProfitMargin)
                {
                    var deal = await _priceAnalysisService.AnalyzeProductAsync(cheapest);
                    deal.EstimatedSellPrice = totalCostMostExpensive;
                    deal.PotentialProfit = potentialProfit;
                    deal.Reasoning = $"Found price discrepancy: Buy for ${totalCostCheapest:F2} ({cheapest.Marketplace}), " +
                                   $"sell for ${totalCostMostExpensive:F2} ({mostExpensive.Marketplace}). " +
                                   $"Profit margin: {profitMargin:F1}%.";

                    deals.Add(deal);
                }
            }

            return deals;
        }

        private async Task<List<Product>> FindSimilarProductsAcrossMarketplaces(Product product)
        {
            var keywords = ExtractKeywords(product.Title);
            var searchQuery = string.Join(" ", keywords.Take(3));

            var similarProducts = new List<Product>();

            // Search other marketplaces for similar products
            var otherMarketplaces = new[] { "eBay", "Facebook Marketplace" }
                .Where(m => !m.Equals(product.Marketplace, StringComparison.OrdinalIgnoreCase));

            foreach (var marketplace in otherMarketplaces)
            {
                var products = await SearchMarketplaceSafely(marketplace, searchQuery, 10);
                var filtered = products.Where(p => IsSimilarProduct(product, p)).ToList();
                similarProducts.AddRange(filtered);
            }

            return similarProducts;
        }

        private List<string> ExtractKeywords(string title)
        {
            if (string.IsNullOrEmpty(title))
                return new List<string>();

            var commonWords = new[] { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "new", "used" };
            
            return title.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !commonWords.Contains(word) && word.Length > 2)
                .ToList();
        }

        private bool IsSimilarProduct(Product product1, Product product2)
        {
            var keywords1 = ExtractKeywords(product1.Title);
            var keywords2 = ExtractKeywords(product2.Title);

            var commonKeywords = keywords1.Intersect(keywords2).Count();
            var totalKeywords = Math.Max(keywords1.Count, keywords2.Count);

            // Consider similar if they share at least 50% of keywords
            return totalKeywords > 0 && (double)commonKeywords / totalKeywords >= 0.5;
        }

        private async Task<List<Deal>> ScanRecentProductsForDeals()
        {
            var deals = new List<Deal>();
            
            var recentProducts = await _context.Products
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddHours(-6))
                .Take(20)
                .ToListAsync();

            foreach (var product in recentProducts)
            {
                var deal = await AnalyzePotentialDealAsync(product);
                if (deal != null && deal.DealScore >= 50)
                {
                    deals.Add(deal);
                }
            }

            return deals;
        }        private List<string> GetTrendingSearchTerms()
        {
            // Focus on highest-value, most commonly searched items for faster discovery
            return new List<string>
            {
                "iPhone",
                "MacBook", 
                "iPad",
                "AirPods",
                "PlayStation",
                "Xbox"
            };
        }

        private async Task<List<Deal>> CreateSyntheticDealsFromSingleMarketplace(List<Product> products, int minProfitMargin)
        {
            _logger.LogInformation("Creating synthetic deals from {Count} products", products.Count);
            var deals = new List<Deal>();

            // Sort products by price to find potential arbitrage opportunities
            var sortedProducts = products.OrderBy(p => p.Price).ToList();

            foreach (var product in sortedProducts.Take(5)) // Limit to top 5 products
            {                try
                {
                    // Estimate potential sell price using price analysis service
                    var estimatedSellPrice = await _priceAnalysisService.EstimateSellingPriceAsync(product);
                    var potentialProfit = estimatedSellPrice - product.Price - product.ShippingCost;
                    var profitMargin = product.Price > 0 ? (potentialProfit / product.Price) * 100 : 0;

                    if (profitMargin >= minProfitMargin && potentialProfit > 10) // At least $10 profit
                    {
                        var deal = new Deal
                        {
                            ProductId = product.Id,
                            Product = product,
                            PotentialProfit = potentialProfit,
                            EstimatedSellPrice = estimatedSellPrice,
                            DealScore = (int)Math.Min(95, profitMargin * 2), // Score based on profit margin
                            Confidence = 75, // Lower confidence since it's synthetic
                            Reasoning = $"Found {product.Title} at ${product.Price:F2} on {product.Marketplace}. " +
                                       $"Estimated sell price: ${estimatedSellPrice:F2}. " +
                                       $"Potential profit: ${potentialProfit:F2} ({profitMargin:F1}% margin)",
                            CreatedAt = DateTime.UtcNow
                        };

                        deals.Add(deal);
                        _logger.LogInformation("Created synthetic deal: {Title} - Profit: ${Profit:F2} ({Margin:F1}%)", 
                            product.Title, potentialProfit, profitMargin);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create synthetic deal for product: {Title}", product.Title);
                }
            }

            return deals;
        }
    }
}
