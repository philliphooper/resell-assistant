using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public NotificationService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task SendDealAlertAsync(Deal deal, SearchAlert alert)
        {
            var product = await _context.Products.FindAsync(deal.ProductId);
            if (product == null) return;

            var subject = $"Deal Alert: {product.Title}";
            var body = $@"
                <h2>New Deal Found!</h2>
                <h3>{product.Title}</h3>
                <p><strong>Price:</strong> ${product.Price:F2}</p>
                <p><strong>Estimated Sell Price:</strong> ${deal.EstimatedSellPrice:F2}</p>
                <p><strong>Potential Profit:</strong> ${deal.PotentialProfit:F2}</p>
                <p><strong>Deal Score:</strong> {deal.DealScore}/100</p>
                <p><strong>Confidence:</strong> {deal.Confidence}%</p>
                <p><strong>Marketplace:</strong> {product.Marketplace}</p>
                <p><strong>Location:</strong> {product.Location}</p>
                <p><strong>Reasoning:</strong> {deal.Reasoning}</p>
                {(string.IsNullOrEmpty(product.Url) ? "" : $"<p><a href='{product.Url}'>View Product</a></p>")}
            ";

            // In a real implementation, you would send to actual email address
            // For now, just log the alert
            Console.WriteLine($"Deal Alert: {subject}");
            
            // Update alert last triggered time
            alert.LastTriggered = DateTime.UtcNow;
            _context.SearchAlerts.Update(alert);
            await _context.SaveChangesAsync();
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Placeholder implementation
            // In a real app, you would use SendGrid, SMTP, etc.
            Console.WriteLine($"Email sent to {to}: {subject}");
            await Task.CompletedTask;
        }

        public async Task ProcessSearchAlertsAsync()
        {
            var activeAlerts = await GetActiveAlertsAsync();
            var marketplaceService = new MarketplaceService(_context);
            var priceAnalysisService = new PriceAnalysisService(_context);

            foreach (var alert in activeAlerts)
            {
                try
                {
                    // Search for products matching the alert
                    var products = await marketplaceService.SearchProductsAsync(alert.SearchQuery, alert.Marketplace);
                    
                    foreach (var product in products)
                    {
                        // Check if this product meets the alert criteria
                        if (await MeetsAlertCriteria(product, alert))
                        {
                            // Analyze the deal
                            var deal = await priceAnalysisService.AnalyzeProductAsync(product);
                            
                            // If it's a good deal, send alert
                            if (deal.DealScore >= 60) // Minimum threshold for alerts
                            {
                                await SendDealAlertAsync(deal, alert);
                                break; // Only send one alert per search query per run
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing alert {alert.Id}: {ex.Message}");
                }
            }
        }

        public async Task<List<SearchAlert>> GetActiveAlertsAsync()
        {
            return await _context.SearchAlerts
                .Where(a => a.IsActive)
                .ToListAsync();
        }

        public async Task CreateAlertAsync(SearchAlert alert)
        {
            alert.CreatedAt = DateTime.UtcNow;
            _context.SearchAlerts.Add(alert);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAlertAsync(SearchAlert alert)
        {
            _context.SearchAlerts.Update(alert);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAlertAsync(int alertId)
        {
            var alert = await _context.SearchAlerts.FindAsync(alertId);
            if (alert != null)
            {
                _context.SearchAlerts.Remove(alert);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<bool> MeetsAlertCriteria(Product product, SearchAlert alert)
        {
            // Check max price constraint
            if (alert.MaxPrice.HasValue && product.Price > alert.MaxPrice.Value)
                return false;

            // Check marketplace constraint
            if (!string.IsNullOrEmpty(alert.Marketplace) && 
                !product.Marketplace.Equals(alert.Marketplace, StringComparison.OrdinalIgnoreCase))
                return false;

            // Check minimum profit constraint
            if (alert.MinProfit.HasValue)
            {
                var priceAnalysisService = new PriceAnalysisService(_context);
                var estimatedSellPrice = await priceAnalysisService.EstimateSellingPriceAsync(product);
                var potentialProfit = estimatedSellPrice - (product.Price + product.ShippingCost);
                
                if (potentialProfit < alert.MinProfit.Value)
                    return false;
            }

            return true;
        }
    }
}
