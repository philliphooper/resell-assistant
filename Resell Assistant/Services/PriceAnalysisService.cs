using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public class PriceAnalysisService : IPriceAnalysisService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarketplaceService _marketplaceService;

        public PriceAnalysisService(ApplicationDbContext context, IMarketplaceService marketplaceService)
        {
            _context = context;
            _marketplaceService = marketplaceService;
        }

        public async Task<List<Deal>> AnalyzeDealsAsync()
        {
            var deals = new List<Deal>();
            var recentProducts = await _context.Products
                .Where(p => p.IsActive && p.DateUpdated > DateTime.UtcNow.AddDays(-7))
                .Include(p => p.PriceHistory)
                .ToListAsync();

            foreach (var product in recentProducts)
            {
                var deal = await AnalyzeSingleProductAsync(product);
                if (deal != null && deal.Score >= 70) // Only good deals
                {
                    deals.Add(deal);
                }
            }

            return deals.OrderByDescending(d => d.Score).ToList();
        }

        public async Task<Deal?> AnalyzeSingleProductAsync(Product product)
        {
            try
            {
                var similarProducts = await _marketplaceService.GetSimilarProductsAsync(product);
                var score = await CalculateDealScoreAsync(product, similarProducts);
                var estimatedSellPrice = await EstimateSellPriceAsync(product);
                var potentialProfit = CalculateProfitMarginAsync(product.Price, estimatedSellPrice, estimatedSellPrice * 0.1m).Result;

                if (score < 50) return null; // Not a good deal

                var reasoning = GenerateReasoning(product, similarProducts, score, estimatedSellPrice);

                var deal = new Deal
                {
                    ProductId = product.Id,
                    Product = product,
                    PotentialProfit = potentialProfit,
                    Score = score,
                    Reasoning = reasoning,
                    IdentifiedDate = DateTime.UtcNow,
                    EstimatedSellPrice = estimatedSellPrice,
                    ConfidenceLevel = CalculateConfidence(similarProducts.Count, score),
                    DaysOnMarket = (DateTime.UtcNow - product.DateListed).Days
                };

                // Save to database
                _context.Deals.Add(deal);
                await _context.SaveChangesAsync();

                return deal;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing product {product.Id}: {ex.Message}");
                return null;
            }
        }

        public async Task<decimal> CalculateDealScoreAsync(Product product, List<Product> similarProducts)
        {
            if (!similarProducts.Any()) return 0;

            var averagePrice = similarProducts.Average(p => p.Price);
            var minPrice = similarProducts.Min(p => p.Price);
            var maxPrice = similarProducts.Max(p => p.Price);

            // Base score: how much below average price
            var priceScore = ((averagePrice - product.Price) / averagePrice) * 100;
            priceScore = Math.Max(0, Math.Min(100, priceScore * 2)); // Scale and cap

            // Bonus points for various factors
            var bonusPoints = 0m;

            // Recently listed (fresher listings might be priced to sell)
            if ((DateTime.UtcNow - product.DateListed).Days <= 1)
                bonusPoints += 10;

            // Price stability (hasn't changed much)
            if (product.PriceHistory.Count >= 2)
            {
                var priceChanges = product.PriceHistory.OrderBy(p => p.Date).ToList();
                var totalChange = Math.Abs(priceChanges.Last().Price - priceChanges.First().Price);
                var changePercent = totalChange / priceChanges.First().Price;
                
                if (changePercent < 0.05m) // Less than 5% price change
                    bonusPoints += 5;
            }

            // Market position
            if (product.Price <= minPrice * 1.1m) // Within 10% of minimum price
                bonusPoints += 15;

            // Final score
            var finalScore = Math.Min(100, priceScore + bonusPoints);
            return Math.Max(0, finalScore);
        }

        public async Task<decimal> EstimateSellPriceAsync(Product product)
        {
            var similarProducts = await _marketplaceService.GetSimilarProductsAsync(product);
            
            if (!similarProducts.Any())
                return product.Price * 1.2m; // Conservative 20% markup

            // Use 75th percentile of similar product prices as estimated sell price
            var sortedPrices = similarProducts.Select(p => p.Price).OrderBy(p => p).ToList();
            var percentile75Index = (int)(sortedPrices.Count * 0.75);
            var estimatedPrice = sortedPrices[Math.Min(percentile75Index, sortedPrices.Count - 1)];

            // Apply marketplace-specific multipliers
            var multiplier = product.Marketplace.ToLower() switch
            {
                "ebay" => 1.1m,      // eBay typically gets higher prices
                "facebook" => 0.95m,  // Facebook Marketplace is more price-sensitive
                "craigslist" => 0.9m, // Craigslist buyers expect deals
                _ => 1.0m
            };

            return estimatedPrice * multiplier;
        }

        public async Task<List<Product>> GetTopDealsAsync(int count = 10)
        {
            var topDeals = await _context.Deals
                .Where(d => d.IsActive && d.IdentifiedDate > DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(d => d.Score)
                .Take(count)
                .Include(d => d.Product)
                .Select(d => d.Product)
                .ToListAsync();

            return topDeals;
        }

        public async Task<decimal> CalculateProfitMarginAsync(decimal buyPrice, decimal sellPrice, decimal fees = 0)
        {
            var grossProfit = sellPrice - buyPrice;
            var netProfit = grossProfit - fees;
            return netProfit;
        }

        private string GenerateReasoning(Product product, List<Product> similarProducts, decimal score, decimal estimatedSellPrice)
        {
            var reasons = new List<string>();

            if (!similarProducts.Any())
            {
                reasons.Add("Limited market data available");
            }
            else
            {
                var averagePrice = similarProducts.Average(p => p.Price);
                var discount = ((averagePrice - product.Price) / averagePrice) * 100;

                if (discount > 20)
                    reasons.Add($"Priced {discount:F1}% below market average");
                else if (discount > 10)
                    reasons.Add($"Good price - {discount:F1}% below average");

                var potentialProfit = estimatedSellPrice - product.Price;
                if (potentialProfit > 50)
                    reasons.Add($"High profit potential: ${potentialProfit:F2}");
                else if (potentialProfit > 20)
                    reasons.Add($"Moderate profit potential: ${potentialProfit:F2}");
            }

            if ((DateTime.UtcNow - product.DateListed).Days <= 1)
                reasons.Add("Recently listed - seller may be motivated");

            if (score >= 90)
                reasons.Add("Exceptional deal - act quickly");
            else if (score >= 80)
                reasons.Add("Very good deal");
            else if (score >= 70)
                reasons.Add("Good deal worth considering");

            return string.Join(". ", reasons) + ".";
        }

        private decimal CalculateConfidence(int similarProductCount, decimal score)
        {
            var baseConfidence = Math.Min(similarProductCount * 10, 80); // More similar products = higher confidence
            var scoreBonus = score / 5; // Score contributes to confidence
            
            return Math.Min(100, baseConfidence + scoreBonus);
        }
    }
}
