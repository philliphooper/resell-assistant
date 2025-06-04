using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public class PriceAnalysisService : IPriceAnalysisService
    {
        private readonly ApplicationDbContext _context;

        public PriceAnalysisService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Deal> AnalyzeProductAsync(Product product)
        {
            var estimatedSellPrice = await EstimateSellingPriceAsync(product);
            var potentialProfit = estimatedSellPrice - (product.Price + product.ShippingCost);
            var dealScore = await CalculateDealScoreAsync(product, estimatedSellPrice);
            var confidence = CalculateConfidence(product, estimatedSellPrice);

            var deal = new Deal
            {
                ProductId = product.Id,
                PotentialProfit = potentialProfit,
                EstimatedSellPrice = estimatedSellPrice,
                DealScore = dealScore,
                Confidence = confidence,
                Reasoning = GenerateReasoning(product, estimatedSellPrice, potentialProfit, dealScore),
                CreatedAt = DateTime.UtcNow
            };

            return deal;
        }

        public async Task<List<Deal>> AnalyzeProductsAsync(List<Product> products)
        {
            var deals = new List<Deal>();
            foreach (var product in products)
            {
                var deal = await AnalyzeProductAsync(product);
                deals.Add(deal);
            }
            return deals.OrderByDescending(d => d.DealScore).ToList();
        }        public async Task<decimal> EstimateSellingPriceAsync(Product product)
        {
            // Enhanced estimation logic with cross-marketplace analysis
            var priceHistory = await GetPriceHistoryAsync(product.Id);
            
            if (priceHistory.Any())
            {
                var recentPrices = priceHistory
                    .Where(p => p.RecordedAt > DateTime.UtcNow.AddDays(-30))
                    .ToList();
                    
                if (recentPrices.Any())
                {
                    var avgPrice = recentPrices.Average(p => p.Price);
                    var maxPrice = recentPrices.Max(p => p.Price);
                    
                    // Use weighted average favoring recent higher prices
                    return (avgPrice * 0.7m) + (maxPrice * 0.3m);
                }
            }

            // Cross-marketplace price analysis
            var similarProducts = await FindSimilarProductsAsync(product);
            if (similarProducts.Any())
            {
                var marketplacePrices = similarProducts
                    .GroupBy(p => p.Marketplace)
                    .Select(g => new { 
                        Marketplace = g.Key, 
                        AvgPrice = g.Average(p => p.Price + p.ShippingCost),
                        MaxPrice = g.Max(p => p.Price + p.ShippingCost)
                    })
                    .ToList();

                if (marketplacePrices.Any())
                {
                    // Target the marketplace with highest average selling price
                    var bestMarketplace = marketplacePrices.OrderByDescending(m => m.AvgPrice).First();
                    return bestMarketplace.AvgPrice * 0.95m; // Slight discount for quick sale
                }
            }

            // Fallback to enhanced basic estimation
            var basePrice = product.Price;
            var conditionMultiplier = GetConditionMultiplier(product.Condition);
            var marketplaceMultiplier = GetMarketplaceMultiplier(product.Marketplace);
            var categoryMultiplier = GetCategoryMultiplier(product.Title);

            return basePrice * conditionMultiplier * marketplaceMultiplier * categoryMultiplier;
        }

        public Task<int> CalculateDealScoreAsync(Product product, decimal estimatedSellPrice)
        {
            var totalCost = product.Price + product.ShippingCost;
            var potentialProfit = estimatedSellPrice - totalCost;
            var profitMargin = totalCost > 0 ? (potentialProfit / totalCost) * 100 : 0;

            var score = 0;

            // Profit margin scoring (0-40 points)
            if (profitMargin >= 50) score += 40;
            else if (profitMargin >= 30) score += 30;
            else if (profitMargin >= 20) score += 20;
            else if (profitMargin >= 10) score += 10;

            // Absolute profit scoring (0-30 points)
            if (potentialProfit >= 200) score += 30;
            else if (potentialProfit >= 100) score += 20;
            else if (potentialProfit >= 50) score += 15;
            else if (potentialProfit >= 25) score += 10;

            // Product demand scoring (0-30 points)
            score += GetDemandScore(product);

            return Task.FromResult(Math.Min(score, 100)); // Cap at 100
        }

        public async Task AddPriceHistoryAsync(int productId, decimal price, string marketplace)
        {
            var priceHistory = new PriceHistory
            {
                ProductId = productId,
                Price = price,
                Marketplace = marketplace,
                RecordedAt = DateTime.UtcNow
            };

            _context.PriceHistories.Add(priceHistory);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PriceHistory>> GetPriceHistoryAsync(int productId)
        {
            return await _context.PriceHistories
                .Where(p => p.ProductId == productId)
                .OrderByDescending(p => p.RecordedAt)
                .ToListAsync();
        }

        private decimal GetConditionMultiplier(string? condition)
        {
            return condition?.ToLower() switch
            {
                "new" => 1.3m,
                "used - excellent" => 1.2m,
                "used - good" => 1.1m,
                "used - fair" => 1.05m,
                _ => 1.1m
            };
        }

        private decimal GetMarketplaceMultiplier(string marketplace)
        {            return marketplace.ToLower() switch
            {
                "ebay" => 1.15m,
                "amazon" => 1.2m,
                // "facebook marketplace" => 1.05m, // Temporarily disabled
                "craigslist" => 1.0m,
                _ => 1.1m
            };
        }

        private int GetDemandScore(Product product)
        {
            var title = product.Title.ToLower();
            
            // High demand items
            if (title.Contains("iphone") || title.Contains("macbook") || title.Contains("airpods"))
                return 30;
            
            if (title.Contains("playstation") || title.Contains("xbox") || title.Contains("nintendo"))
                return 25;
            
            if (title.Contains("laptop") || title.Contains("tablet") || title.Contains("watch"))
                return 20;
            
            return 15; // Default moderate demand
        }

        private int CalculateConfidence(Product product, decimal estimatedSellPrice)
        {
            var confidence = 50; // Base confidence

            // Higher confidence for well-known brands
            var title = product.Title.ToLower();
            if (title.Contains("apple") || title.Contains("samsung") || title.Contains("sony"))
                confidence += 20;

            // Higher confidence for detailed descriptions
            if (!string.IsNullOrEmpty(product.Description) && product.Description.Length > 100)
                confidence += 10;

            // Higher confidence for items with images
            if (!string.IsNullOrEmpty(product.ImageUrl))
                confidence += 10;

            // Lower confidence for very high profit margins (too good to be true)
            var profitMargin = (estimatedSellPrice - product.Price) / product.Price * 100;
            if (profitMargin > 100)
                confidence -= 20;

            return Math.Max(Math.Min(confidence, 100), 0);
        }        private string GenerateReasoning(Product product, decimal estimatedSellPrice, decimal potentialProfit, int dealScore)
        {
            var reasons = new List<string>();
            
            var profitMargin = (potentialProfit / product.Price) * 100;
            
            if (profitMargin >= 30)
                reasons.Add($"High profit margin of {profitMargin:F1}%");
            else if (profitMargin >= 15)
                reasons.Add($"Good profit margin of {profitMargin:F1}%");
            
            if (potentialProfit >= 100)
                reasons.Add($"Strong absolute profit of ${potentialProfit:F2}");
            
            var title = product.Title.ToLower();
            if (title.Contains("iphone") || title.Contains("macbook"))
                reasons.Add("High-demand Apple product with strong resale value");
            else if (title.Contains("playstation") || title.Contains("xbox"))
                reasons.Add("Popular gaming console with consistent demand");
            
            if (product.Condition?.ToLower().Contains("new") == true)
                reasons.Add("New condition increases selling potential");
            
            if (dealScore >= 80)
                reasons.Add("Exceptional deal score indicates strong opportunity");
            else if (dealScore >= 60)
                reasons.Add("Good deal score with solid profit potential");
            
            return string.Join(". ", reasons);
        }

        private async Task<List<Product>> FindSimilarProductsAsync(Product product)
        {
            var keywords = ExtractKeywords(product.Title);
            var primaryKeywords = keywords.Take(3).ToList();

            if (!primaryKeywords.Any())
                return new List<Product>();

            var searchTerms = string.Join(" ", primaryKeywords);
            
            // Search for similar products in database
            var similarProducts = await _context.Products
                .Where(p => p.Id != product.Id && 
                           p.Title.ToLower().Contains(searchTerms.ToLower()))
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-90)) // Recent listings only
                .Take(20)
                .ToListAsync();

            // Filter by similarity score
            return similarProducts
                .Where(p => CalculateSimilarityScore(product.Title, p.Title) >= 0.4)
                .ToList();
        }

        private List<string> ExtractKeywords(string title)
        {
            if (string.IsNullOrEmpty(title))
                return new List<string>();

            var commonWords = new[] { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "new", "used", "excellent", "good", "fair" };
            
            return title.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !commonWords.Contains(word) && word.Length > 2)
                .Distinct()
                .ToList();
        }

        private double CalculateSimilarityScore(string title1, string title2)
        {
            var keywords1 = ExtractKeywords(title1);
            var keywords2 = ExtractKeywords(title2);

            if (!keywords1.Any() || !keywords2.Any())
                return 0;

            var commonKeywords = keywords1.Intersect(keywords2).Count();
            var totalKeywords = keywords1.Union(keywords2).Count();

            return (double)commonKeywords / totalKeywords;
        }

        private decimal GetCategoryMultiplier(string title)
        {
            var titleLower = title.ToLower();
            
            // Electronics categories with high resale value
            if (titleLower.Contains("iphone") || titleLower.Contains("airpods") || titleLower.Contains("macbook"))
                return 1.4m;
                
            if (titleLower.Contains("playstation") || titleLower.Contains("xbox") || titleLower.Contains("nintendo"))
                return 1.3m;
                
            if (titleLower.Contains("laptop") || titleLower.Contains("tablet") || titleLower.Contains("camera"))
                return 1.25m;
                
            if (titleLower.Contains("watch") || titleLower.Contains("headphones") || titleLower.Contains("speaker"))
                return 1.2m;
                
            // Fashion and accessories
            if (titleLower.Contains("designer") || titleLower.Contains("luxury") || titleLower.Contains("vintage"))
                return 1.3m;
                
            return 1.1m; // Default multiplier
        }
    }
}
