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

        public async Task SendDealAlertAsync(Deal deal, string email)
        {
            var subject = $"Great Deal Found: {deal.Product.Title}";
            var body = $@"
                <h2>Resell Opportunity Alert!</h2>
                <h3>{deal.Product.Title}</h3>
                <p><strong>Current Price:</strong> ${deal.Product.Price:F2}</p>
                <p><strong>Estimated Sell Price:</strong> ${deal.EstimatedSellPrice:F2}</p>
                <p><strong>Potential Profit:</strong> ${deal.PotentialProfit:F2}</p>
                <p><strong>Deal Score:</strong> {deal.Score:F1}/100</p>
                <p><strong>Marketplace:</strong> {deal.Product.Marketplace}</p>
                <p><strong>Reasoning:</strong> {deal.Reasoning}</p>
                <p><strong>Confidence Level:</strong> {deal.ConfidenceLevel:F1}%</p>
                
                {(string.IsNullOrEmpty(deal.Product.ProductUrl) ? "" : $"<p><a href='{deal.Product.ProductUrl}'>View Item</a></p>")}
                
                <p><em>Act quickly - good deals don't last long!</em></p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPriceDropAlertAsync(Product product, decimal oldPrice, string email)
        {
            var priceDropPercent = ((oldPrice - product.Price) / oldPrice) * 100;
            var subject = $"Price Drop Alert: {product.Title}";
            var body = $@"
                <h2>Price Drop Detected!</h2>
                <h3>{product.Title}</h3>
                <p><strong>Old Price:</strong> ${oldPrice:F2}</p>
                <p><strong>New Price:</strong> ${product.Price:F2}</p>
                <p><strong>Price Drop:</strong> ${oldPrice - product.Price:F2} ({priceDropPercent:F1}%)</p>
                <p><strong>Marketplace:</strong> {product.Marketplace}</p>
                
                {(string.IsNullOrEmpty(product.ProductUrl) ? "" : $"<p><a href='{product.ProductUrl}'>View Item</a></p>")}
                
                <p><em>This could be a great buying opportunity!</em></p>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task CheckAndSendAlertsAsync()
        {
            // Get all active search alerts
            var alerts = await _context.SearchAlerts
                .Where(a => a.IsActive && !string.IsNullOrEmpty(a.EmailNotification))
                .ToListAsync();

            foreach (var alert in alerts)
            {
                try
                {
                    // Check for new deals matching the alert criteria
                    var matchingDeals = await _context.Deals
                        .Include(d => d.Product)
                        .Where(d => d.IsActive && 
                                   d.IdentifiedDate > (alert.LastTriggered ?? DateTime.MinValue) &&
                                   d.Product.Title.Contains(alert.SearchQuery))
                        .Where(d => !alert.MaxPrice.HasValue || d.Product.Price <= alert.MaxPrice)
                        .Where(d => !alert.MinProfit.HasValue || d.PotentialProfit >= alert.MinProfit)
                        .ToListAsync();

                    // Filter by marketplace if specified
                    if (alert.Marketplaces.Any())
                    {
                        matchingDeals = matchingDeals
                            .Where(d => alert.Marketplaces.Contains(d.Product.Marketplace))
                            .ToList();
                    }

                    // Send notifications for new deals
                    foreach (var deal in matchingDeals)
                    {
                        await SendDealAlertAsync(deal, alert.EmailNotification!);
                    }

                    // Update last triggered time
                    if (matchingDeals.Any())
                    {
                        alert.LastTriggered = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing alert {alert.Id}: {ex.Message}");
                }
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                // In a production environment, you would integrate with an email service
                // like SendGrid, AWS SES, or SMTP
                
                // For now, we'll just log the email content
                Console.WriteLine($"EMAIL NOTIFICATION:");
                Console.WriteLine($"To: {to}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body: {body}");
                Console.WriteLine($"---");

                // TODO: Implement actual email sending
                // Example with SMTP:
                /*
                using var client = new SmtpClient();
                client.Host = _configuration["Email:SmtpHost"];
                client.Port = int.Parse(_configuration["Email:SmtpPort"]);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(
                    _configuration["Email:Username"], 
                    _configuration["Email:Password"]);

                var message = new MailMessage();
                message.From = new MailAddress(_configuration["Email:FromAddress"]);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                await client.SendMailAsync(message);
                */

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
