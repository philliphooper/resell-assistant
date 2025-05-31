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
        }

        public async Task<decimal> EstimateSellingPriceAsync(Product product)
        {
            // Simple estimation logic - in a real app, this would use market data
            var priceHistory = await GetPriceHistoryAsync(product.Id);
            
            if (priceHistory.Any())
            {
                var avgPrice = priceHistory.Average(p => p.Price);
                return avgPrice * 1.1m; // Add 10% markup
            }

            // Basic estimation based on product type and condition
            var basePrice = product.Price;
            var conditionMultiplier = GetConditionMultiplier(product.Condition);
            var marketplaceMultiplier = GetMarketplaceMultiplier(product.Marketplace);

            return basePrice * conditionMultiplier * marketplaceMultiplier;
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
        {
            return marketplace.ToLower() switch
            {
                "ebay" => 1.15m,
                "amazon" => 1.2m,
                "facebook marketplace" => 1.05m,
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
        }

        private string GenerateReasoning(Product product, decimal estimatedSellPrice, decimal potentialProfit, int dealScore)
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
    }
}
