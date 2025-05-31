using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public interface INotificationService
    {
        Task SendDealAlertAsync(Deal deal, string email);
        Task SendPriceDropAlertAsync(Product product, decimal oldPrice, string email);
        Task CheckAndSendAlertsAsync();
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}
