using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;
using Resell_Assistant.DTOs;
using Resell_Assistant.Services.External;

namespace Resell_Assistant.Services
{    public interface IDealDiscoveryService
    {
        // Legacy methods (kept for backward compatibility during transition)
        Task<List<Deal>> DiscoverCrossMarketplaceDealsAsync(int maxResults = 20);
        Task<List<Deal>> FindPriceDiscrepanciesAsync(string query, int minProfitMargin = 15, int maxSearchResults = 10);
        Task<Deal?> AnalyzePotentialDealAsync(Product product);
        Task<List<Deal>> ScanForRealTimeDealsAsync();

        // New refactored methods for intelligent product discovery
        Task<List<Deal>> DiscoverIntelligentDealsAsync(DealDiscoverySettingsDto settings, IProgress<DiscoveryProgressDto>? progress = null);
        Task<List<Product>> FindTrendingProductsAsync(int count, string? searchTerms = null);
        Task<List<ComparisonListing>> FindListingsForProductAsync(Product product, int maxListings, List<string> marketplaces);
        Task<Deal> CreateDealFromListingsAsync(List<ComparisonListing> listings, DealDiscoverySettingsDto settings);
        Task<bool> ValidateExactResultCountAsync(int requestedCount);
    }

    public class DealDiscoveryService : IDealDiscoveryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarketplaceService _marketplaceService;
        private readonly IPriceAnalysisService _priceAnalysisService;
        private readonly IEbayApiService _ebayApiService;
        private readonly IFacebookMarketplaceService _facebookMarketplaceService;
        private readonly ILogger<DealDiscoveryService> _logger;        public DealDiscoveryService(
            ApplicationDbContext context,
            IMarketplaceService marketplaceService,
            IPriceAnalysisService priceAnalysisService,
            IEbayApiService ebayApiService,
            ILogger<DealDiscoveryService> logger)
        {
            _context = context;
            _marketplaceService = marketplaceService;
            _priceAnalysisService = priceAnalysisService;
            _ebayApiService = ebayApiService;
            _facebookMarketplaceService = null!; // Temporarily disabled
            _logger = logger;
        }public async Task<List<Deal>> DiscoverCrossMarketplaceDealsAsync(int maxResults = 20)
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
            }            // Include recent products scan now that it uses APIs
            var recentDeals = await ScanRecentProductsForDeals();
            allDeals.AddRange(recentDeals);

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

            var deals = new List<Deal>();            // Search eBay only (Facebook Marketplace temporarily disabled)
            var ebayProducts = await SearchMarketplaceSafely("eBay", query, maxSearchResults);
            var facebookProducts = new List<Product>(); // Facebook Marketplace temporarily disabled

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
        }        public async Task<List<Deal>> ScanForRealTimeDealsAsync()
        {
            _logger.LogInformation("Scanning for real-time deals using marketplace APIs");

            var deals = new List<Deal>();

            // Get trending search terms for real-time scanning
            var trendingTerms = GetTrendingSearchTerms().Take(5).ToList();

            foreach (var term in trendingTerms)
            {
                try
                {                    // Search eBay only (Facebook Marketplace temporarily disabled)
                    var ebayProducts = await SearchMarketplaceSafely("eBay", term, 20);
                    var facebookProducts = new List<Product>(); // Facebook Marketplace temporarily disabled

                    var allProducts = ebayProducts.Concat(facebookProducts).ToList();

                    // Analyze each product for potential deals
                    foreach (var product in allProducts.Take(10)) // Limit for performance
                    {
                        var deal = await AnalyzePotentialDealAsync(product);
                        if (deal != null && deal.DealScore >= 60)
                        {
                            deals.Add(deal);
                        }
                    }

                    // Break early if we have enough high-quality deals
                    if (deals.Count >= 20) break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan real-time deals for term: {Term}", term);
                }
            }

            return deals.OrderByDescending(d => d.DealScore).Take(20).ToList();
        }

        // New refactored methods for intelligent product discovery
        public async Task<List<Deal>> DiscoverIntelligentDealsAsync(DealDiscoverySettingsDto settings, IProgress<DiscoveryProgressDto>? progress = null)
        {
            _logger.LogInformation("Starting intelligent deal discovery with exact result count: {Count}", settings.ExactResultCount);
            
            var deals = new List<Deal>();
            var discoveredProducts = new HashSet<int>(); // Track unique products
            
            progress?.Report(new DiscoveryProgressDto 
            { 
                CurrentPhase = "Initialization",
                CurrentAction = "Setting up discovery process",
                PercentComplete = 0
            });

            try
            {
                // Phase 1: Find trending products
                progress?.Report(new DiscoveryProgressDto 
                { 
                    CurrentPhase = "Product Discovery",
                    CurrentAction = "Finding trending products",
                    PercentComplete = 10
                });

                var trendingProducts = await FindTrendingProductsAsync(settings.UniqueProductCount, settings.SearchTerms);
                
                progress?.Report(new DiscoveryProgressDto 
                { 
                    CurrentPhase = "Product Discovery",
                    CurrentAction = $"Found {trendingProducts.Count} trending products",
                    ProductsFound = trendingProducts.Count,
                    PercentComplete = 25
                });

                // Phase 2: For each product, find multiple listings
                for (int i = 0; i < trendingProducts.Count && deals.Count < settings.ExactResultCount; i++)
                {
                    var product = trendingProducts[i];
                    
                    if (discoveredProducts.Contains(product.Id))
                        continue;

                    progress?.Report(new DiscoveryProgressDto 
                    { 
                        CurrentPhase = "Listing Analysis",
                        CurrentAction = $"Analyzing listings for: {product.Title.Substring(0, Math.Min(50, product.Title.Length))}...",
                        ProductsFound = trendingProducts.Count,
                        PercentComplete = 25 + (i * 50 / trendingProducts.Count)
                    });

                    // Find multiple listings for this product
                    var listings = await FindListingsForProductAsync(product, settings.ListingsPerProduct, settings.PreferredMarketplaces);
                    
                    if (listings.Count >= 2) // Need at least 2 listings for comparison
                    {
                        var deal = await CreateDealFromListingsAsync(listings, settings);
                        if (deal != null && deal.PotentialProfit > 0)
                        {
                            deals.Add(deal);
                            discoveredProducts.Add(product.Id);
                            
                            progress?.Report(new DiscoveryProgressDto 
                            { 
                                CurrentPhase = "Deal Creation",
                                CurrentAction = $"Created deal for {product.Title.Substring(0, Math.Min(30, product.Title.Length))}...",
                                ProductsFound = trendingProducts.Count,
                                ListingsAnalyzed = deals.Sum(d => d.TotalListingsAnalyzed),
                                DealsCreated = deals.Count,
                                PercentComplete = 25 + (i * 50 / trendingProducts.Count),
                                RecentFindings = new() { $"${deal.PotentialProfit:F2} profit on {product.Title.Substring(0, Math.Min(40, product.Title.Length))}" }
                            });
                        }
                    }
                }

                // Phase 3: Ensure exact result count
                progress?.Report(new DiscoveryProgressDto 
                { 
                    CurrentPhase = "Finalization",
                    CurrentAction = "Ensuring exact result count",
                    PercentComplete = 85
                });

                deals = await EnsureExactResultCountAsync(deals, settings, discoveredProducts);

                progress?.Report(new DiscoveryProgressDto 
                { 
                    CurrentPhase = "Complete",
                    CurrentAction = $"Discovery complete with {deals.Count} deals",
                    ProductsFound = trendingProducts.Count,
                    ListingsAnalyzed = deals.Sum(d => d.TotalListingsAnalyzed),
                    DealsCreated = deals.Count,
                    PercentComplete = 100
                });

                _logger.LogInformation("Intelligent discovery completed: {DealCount} deals created from {ProductCount} products", 
                    deals.Count, trendingProducts.Count);
                
                return deals.OrderByDescending(d => d.DealScore).Take(settings.ExactResultCount).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during intelligent deal discovery");
                progress?.Report(new DiscoveryProgressDto 
                { 
                    CurrentPhase = "Error",
                    CurrentAction = "Discovery failed: " + ex.Message,
                    PercentComplete = 0
                });
                throw;
            }
        }

        public async Task<List<Product>> FindTrendingProductsAsync(int count, string? searchTerms = null)
        {
            _logger.LogInformation("Finding {Count} trending products", count);
            
            var products = new List<Product>();
            var searchQueries = new List<string>();

            // Use provided search terms or default trending terms
            if (!string.IsNullOrEmpty(searchTerms))
            {
                searchQueries.AddRange(searchTerms.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()));
            }
            else
            {
                searchQueries.AddRange(GetExpandedTrendingSearchTerms());
            }

            foreach (var query in searchQueries.Take(10)) // Limit to 10 search terms
            {
                if (products.Count >= count) break;                try
                {
                    var queryProducts = await SearchMarketplaceSafely("eBay", query, Math.Min(20, count));
                    var facebookProducts = new List<Product>(); // Facebook Marketplace temporarily disabled
                    
                    var allProducts = queryProducts.Concat(facebookProducts)
                        .Where(p => !products.Any(existing => existing.Id == p.Id))
                        .OrderByDescending(p => CalculateTrendingScore(p))
                        .Take(count - products.Count);
                    
                    products.AddRange(allProducts);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to search for trending products with query: {Query}", query);
                }
            }

            return products.DistinctBy(p => NormalizeProductTitle(p.Title)).Take(count).ToList();
        }

        public async Task<List<ComparisonListing>> FindListingsForProductAsync(Product product, int maxListings, List<string> marketplaces)
        {
            _logger.LogInformation("Finding {MaxListings} listings for product: {ProductTitle}", maxListings, product.Title);
            
            var listings = new List<ComparisonListing>();
            var keywords = ExtractKeywords(product.Title);
            var searchQuery = string.Join(" ", keywords.Take(5)); // Use top 5 keywords

            foreach (var marketplace in marketplaces)
            {
                try
                {
                    var marketplaceProducts = await SearchMarketplaceSafely(marketplace, searchQuery, maxListings * 2);
                    
                    var similarProducts = marketplaceProducts
                        .Where(p => IsSimilarProduct(product, p))
                        .OrderBy(p => p.Price)
                        .Take(maxListings / marketplaces.Count + 1)
                        .ToList();

                    foreach (var similarProduct in similarProducts)
                    {
                        var comparisonListing = new ComparisonListing
                        {
                            ProductId = similarProduct.Id,
                            Title = similarProduct.Title,
                            Price = similarProduct.Price,
                            ShippingCost = similarProduct.ShippingCost,
                            Marketplace = similarProduct.Marketplace,
                            Condition = similarProduct.Condition,
                            Location = similarProduct.Location,
                            Url = similarProduct.Url,
                            ImageUrl = similarProduct.ImageUrl,
                            DateListed = similarProduct.CreatedAt,
                            IsSelectedDeal = false, // Will be set later
                            RankingPosition = listings.Count + 1,
                            CreatedAt = DateTime.UtcNow
                        };

                        listings.Add(comparisonListing);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to find listings on {Marketplace} for product: {ProductTitle}", 
                        marketplace, product.Title);
                }
            }

            return listings.OrderBy(l => l.Price + l.ShippingCost).Take(maxListings).ToList();
        }

        public async Task<Deal> CreateDealFromListingsAsync(List<ComparisonListing> listings, DealDiscoverySettingsDto settings)
        {
            if (!listings.Any())
                throw new ArgumentException("Cannot create deal without listings");

            // Find the best deal (lowest cost) and highest value listing
            var cheapestListing = listings.OrderBy(l => l.Price + l.ShippingCost).First();
            var highestListing = listings.OrderByDescending(l => l.Price + l.ShippingCost).First();
            
            cheapestListing.IsSelectedDeal = true;

            // Calculate potential profit
            var buyCost = cheapestListing.Price + cheapestListing.ShippingCost;
            var estimatedSellPrice = highestListing.Price;
            var potentialProfit = estimatedSellPrice - buyCost;
            var profitMargin = buyCost > 0 ? (potentialProfit / buyCost) * 100 : 0;            // Calculate deal score based on profit margin, number of listings, and marketplace diversity
            var marketplaceDiversity = listings.Select(l => l.Marketplace).Distinct().Count();
            var dealScore = CalculateDealScore((double)profitMargin, listings.Count, marketplaceDiversity);            // Create in-memory product for the cheapest listing (no database persistence)
            var product = CreateProductFromListing(cheapestListing);

            var deal = new Deal
            {
                ProductId = product.Id,
                Product = product,
                PotentialProfit = Math.Round(potentialProfit, 2),
                EstimatedSellPrice = Math.Round(estimatedSellPrice, 2),
                DealScore = dealScore,
                Confidence = CalculateConfidence(listings.Count, marketplaceDiversity),
                TotalListingsAnalyzed = listings.Count,
                DiscoveryMethod = $"Intelligent Discovery: Analyzed {listings.Count} listings across {marketplaceDiversity} marketplaces. " +
                                 $"Buy from {cheapestListing.Marketplace} at ${buyCost:F2}, sell potential ${estimatedSellPrice:F2}",
                Reasoning = $"Found {cheapestListing.Title} available for ${buyCost:F2} (including shipping) on {cheapestListing.Marketplace}. " +
                           $"Similar items selling for up to ${estimatedSellPrice:F2}. " +
                           $"Potential profit: ${potentialProfit:F2} ({profitMargin:F1}% margin). " +
                           $"Analysis based on {listings.Count} comparable listings.",
                CreatedAt = DateTime.UtcNow,
                ComparisonListings = listings,
                Id = Guid.NewGuid().ToString() // Generate unique ID for in-memory deal
            };

            _logger.LogInformation("Created in-memory deal with score {DealScore} and potential profit ${PotentialProfit:F2}", 
                deal.DealScore, deal.PotentialProfit);

            return deal;
        }        public async Task<bool> ValidateExactResultCountAsync(int requestedCount)
        {
            // Validate based on marketplace API availability rather than database content
            // Since we're using real-time APIs, we can potentially fulfill most reasonable requests
            
            try
            {                // Test with a sample search to see if marketplaces are responsive
                var testQuery = "iPhone"; // Use a common search term
                var testResults = await SearchMarketplaceSafely("eBay", testQuery, 5);
                var facebookTestResults = new List<Product>(); // Facebook Marketplace temporarily disabled
                
                var totalTestResults = testResults.Count + facebookTestResults.Count;
                
                // If we can get at least some results from a test search, 
                // and the requested count is reasonable, we should be able to fulfill it
                var canFulfill = totalTestResults > 0 && requestedCount <= 100; // Cap at 100 for performance
                
                _logger.LogInformation("Validation check: Test results: {TestResults}, Requested: {RequestedCount}, Can fulfill: {CanFulfill}", 
                    totalTestResults, requestedCount, canFulfill);
                
                return canFulfill;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate exact result count, assuming we can fulfill request");
                // If validation fails, assume we can try (better to attempt than fail immediately)
                return requestedCount <= 50; // Conservative limit if validation fails
            }
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

            var similarProducts = new List<Product>();            // Search other marketplaces for similar products
            var otherMarketplaces = new[] { "eBay" } // Facebook Marketplace temporarily disabled
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
        }        private async Task<List<Deal>> ScanRecentProductsForDeals()
        {
            var deals = new List<Deal>();
            
            // Use marketplace APIs instead of database queries
            var trendingTerms = GetTrendingSearchTerms().Take(3).ToList();
            
            foreach (var term in trendingTerms)
            {                try
                {
                    // Search for recent listings from marketplace APIs (eBay only for now)
                    var ebayProducts = await SearchMarketplaceSafely("eBay", term, 10);
                    var facebookProducts = new List<Product>(); // Facebook Marketplace temporarily disabled
                    
                    var recentProducts = ebayProducts.Concat(facebookProducts)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(15)
                        .ToList();

                    foreach (var product in recentProducts)
                    {
                        var deal = await AnalyzePotentialDealAsync(product);
                        if (deal != null && deal.DealScore >= 50)
                        {
                            deals.Add(deal);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan recent products for term: {Term}", term);
                }
            }

            return deals.Take(20).ToList();
        }private List<string> GetTrendingSearchTerms()
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

        // Helper methods for the new intelligent discovery system
        private async Task<List<Deal>> EnsureExactResultCountAsync(List<Deal> currentDeals, DealDiscoverySettingsDto settings, HashSet<int> discoveredProducts)
        {
            if (currentDeals.Count >= settings.ExactResultCount)
            {
                return currentDeals.OrderByDescending(d => d.DealScore).Take(settings.ExactResultCount).ToList();
            }

            // If we don't have enough deals, try to find more with relaxed criteria
            var additionalNeeded = settings.ExactResultCount - currentDeals.Count;
            _logger.LogInformation("Need {Additional} more deals to reach exact count", additionalNeeded);

            // Try with expanded search terms and lower profit margins
            var expandedSettings = new DealDiscoverySettingsDto
            {
                ExactResultCount = additionalNeeded,
                UniqueProductCount = additionalNeeded * 2,
                ListingsPerProduct = settings.ListingsPerProduct,
                MinProfitMargin = Math.Max(5, settings.MinProfitMargin - 5), // Lower margin
                PreferredMarketplaces = settings.PreferredMarketplaces
            };

            var additionalProducts = await FindTrendingProductsAsync(additionalNeeded * 3, null);
            
            foreach (var product in additionalProducts.Where(p => !discoveredProducts.Contains(p.Id)).Take(additionalNeeded))
            {
                var listings = await FindListingsForProductAsync(product, settings.ListingsPerProduct, settings.PreferredMarketplaces);
                
                if (listings.Count >= 2)
                {
                    var deal = await CreateDealFromListingsAsync(listings, expandedSettings);
                    if (deal != null && deal.PotentialProfit > 0)
                    {
                        currentDeals.Add(deal);
                        discoveredProducts.Add(product.Id);
                        
                        if (currentDeals.Count >= settings.ExactResultCount)
                            break;
                    }
                }
            }

            return currentDeals.OrderByDescending(d => d.DealScore).Take(settings.ExactResultCount).ToList();
        }

        private List<string> GetExpandedTrendingSearchTerms()
        {
            return new List<string>
            {
                // Electronics
                "iPhone", "Samsung Galaxy", "iPad", "MacBook", "Dell Laptop", "HP Laptop",
                "AirPods", "Sony Headphones", "Apple Watch", "Fitbit", "Nintendo Switch",
                "PlayStation", "Xbox", "Gaming Chair", "Monitor", "Keyboard",
                
                // Fashion & Accessories
                "Designer Handbag", "Luxury Watch", "Sneakers", "Jordan", "Nike", "Adidas",
                "Vintage Clothing", "Designer Shoes", "Sunglasses", "Jewelry",
                
                // Home & Garden
                "Coffee Machine", "Vacuum Cleaner", "Air Fryer", "Instant Pot", "Blender",
                "Garden Tools", "Power Tools", "Furniture", "Decor",
                
                // Sports & Fitness
                "Bicycle", "Treadmill", "Weights", "Yoga Mat", "Exercise Equipment",
                
                // Collectibles
                "Pokemon Cards", "Baseball Cards", "Comic Books", "Vinyl Records", "Vintage Toys"
            };
        }

        private double CalculateTrendingScore(Product product)
        {
            var score = 0.0;
            
            // Higher score for recent listings
            var daysSinceCreated = (DateTime.UtcNow - product.CreatedAt).TotalDays;
            score += Math.Max(0, 10 - daysSinceCreated); // Up to 10 points for recent items
            
            // Score based on price range (sweet spot between $50-$500)
            if (product.Price >= 50 && product.Price <= 500)
                score += 5;
            else if (product.Price >= 20 && product.Price <= 1000)
                score += 3;
            
            // Bonus for popular keywords
            var title = product.Title.ToLower();
            var popularKeywords = new[] { "iphone", "macbook", "ipad", "airpods", "nintendo", "playstation", "xbox", "jordan", "nike" };
            score += popularKeywords.Count(keyword => title.Contains(keyword)) * 2;
              // Marketplace diversity bonus
            if (product.Marketplace == "eBay") score += 1;
            // if (product.Marketplace == "Facebook Marketplace") score += 1; // Temporarily disabled
            
            return score;
        }

        private int CalculateDealScore(double profitMargin, int listingCount, int marketplaceDiversity)
        {
            var score = 0;
            
            // Base score from profit margin
            score += (int)Math.Min(50, profitMargin * 2);
            
            // Bonus for more listings (better confidence)
            score += Math.Min(20, listingCount * 2);
            
            // Bonus for marketplace diversity
            score += Math.Min(15, marketplaceDiversity * 5);
            
            // Additional bonus for very high profit margins
            if (profitMargin > 50) score += 10;
            if (profitMargin > 100) score += 5;
            
            return Math.Min(100, Math.Max(0, score));
        }

        private int CalculateConfidence(int listingCount, int marketplaceDiversity)
        {
            var confidence = 50; // Base confidence
            
            // More listings = higher confidence
            confidence += Math.Min(30, listingCount * 3);
            
            // More marketplaces = higher confidence
            confidence += Math.Min(20, marketplaceDiversity * 8);
            
            return Math.Min(100, Math.Max(0, confidence));
        }

        private async Task<Product> GetOrCreateProductAsync(ComparisonListing listing)
        {
            // Try to find existing product by normalized title and marketplace
            var normalizedTitle = NormalizeProductTitle(listing.Title);
            
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Title == listing.Title && p.Marketplace == listing.Marketplace);

            if (existingProduct != null)
                return existingProduct;

            // Create new product
            var newProduct = new Product
            {
                Title = listing.Title,
                Price = listing.Price,
                ShippingCost = listing.ShippingCost,
                Marketplace = listing.Marketplace,
                Condition = listing.Condition,
                Location = listing.Location,
                Url = listing.Url,
                ImageUrl = listing.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();
            
            return newProduct;
        }

        private async Task SaveDealWithComparisonListingsAsync(Deal deal)
        {
            // Add the deal to context
            _context.Deals.Add(deal);
            await _context.SaveChangesAsync();

            // Update comparison listings with the deal ID
            foreach (var listing in deal.ComparisonListings)
            {
                listing.DealId = deal.Id;
                _context.ComparisonListings.Add(listing);
            }

            await _context.SaveChangesAsync();
        }
    }
}
