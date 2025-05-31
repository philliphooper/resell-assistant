using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public interface INotificationService
    {
        Task SendDealAlertAsync(Deal deal, SearchAlert alert);
        Task SendEmailAsync(string to, string subject, string body);
        Task ProcessSearchAlertsAsync();
        Task<List<SearchAlert>> GetActiveAlertsAsync();
        Task CreateAlertAsync(SearchAlert alert);
        Task UpdateAlertAsync(SearchAlert alert);
        Task DeleteAlertAsync(int alertId);
    }
}
