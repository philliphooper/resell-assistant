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
            
            // Note: For now we'll skip processing since MarketplaceService requires external API services
            // This would be properly implemented with background service DI in a production app
            
            Console.WriteLine($"Processing {activeAlerts.Count} active search alerts (stub implementation)");

            foreach (var alert in activeAlerts)
            {
                try
                {
                    // For now, just log that we would process this alert
                    Console.WriteLine($"Would process alert {alert.Id} for query: {alert.SearchQuery}");
                    
                    // TODO: Implement proper alert processing with injected MarketplaceService
                    // - Search for products matching the alert
                    // - Check if products meet alert criteria
                    // - Analyze deals and send notifications
                    
                    await Task.Delay(100); // Simulate processing time
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
        }        private Task<bool> MeetsAlertCriteria(Product product, SearchAlert alert)
        {
            // Check max price constraint
            if (alert.MaxPrice.HasValue && product.Price > alert.MaxPrice.Value)
                return Task.FromResult(false);

            // Check marketplace constraint
            if (!string.IsNullOrEmpty(alert.Marketplace) && 
                !product.Marketplace.Equals(alert.Marketplace, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(false);

            // Check minimum profit constraint (simplified for now)
            if (alert.MinProfit.HasValue)
            {
                // TODO: This needs proper price analysis service with DI
                // For now, use a simple estimate
                var estimatedSellPrice = product.Price * 1.2m; // 20% markup estimate
                var potentialProfit = estimatedSellPrice - (product.Price + product.ShippingCost);
                
                if (potentialProfit < alert.MinProfit.Value)
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
