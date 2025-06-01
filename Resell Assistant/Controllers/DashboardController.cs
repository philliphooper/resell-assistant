using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;

namespace Resell_Assistant.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            try
            {
                // Get counts
                var totalProducts = await _context.Products.CountAsync();
                var totalDeals = await _context.Deals.CountAsync();

                // Calculate total profit from deals
                var totalProfit = await _context.Deals.SumAsync(d => d.PotentialProfit);

                // Calculate average deal score
                var averageDealScore = totalDeals > 0 
                    ? await _context.Deals.AverageAsync(d => d.DealScore)
                    : 0;

                // Find top marketplace
                var topMarketplace = await _context.Products
                    .GroupBy(p => p.Marketplace)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync() ?? "N/A";

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

                // Get top categories from product titles (using simple keyword matching)
                var allProducts = await _context.Products.Select(p => p.Title).ToListAsync();
                var categoryKeywords = new Dictionary<string, string[]>
                {
                    { "Electronics", new[] { "iPhone", "iPad", "MacBook", "PlayStation", "Switch", "Samsung", "Apple", "Watch", "AirPods" } },
                    { "Gaming", new[] { "PlayStation", "Xbox", "Nintendo", "Switch", "PS5", "Console", "Gaming" } },
                    { "Mobile", new[] { "iPhone", "Samsung", "Galaxy", "Phone", "Mobile" } },
                    { "Computers", new[] { "MacBook", "Laptop", "Computer", "PC", "iMac" } },
                    { "Audio", new[] { "AirPods", "Headphones", "Speakers", "Audio", "Beats" } }
                };

                var categoryCount = new Dictionary<string, int>();
                foreach (var product in allProducts)
                {
                    foreach (var category in categoryKeywords)
                    {
                        if (category.Value.Any(keyword => product.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
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
                    .ToList();                // Get recent deals with product details
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
                    recentDeals
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to calculate dashboard stats", error = ex.Message });
            }
        }
    }
}