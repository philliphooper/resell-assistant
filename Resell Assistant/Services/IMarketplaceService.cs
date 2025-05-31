using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public interface IMarketplaceService
    {
        Task<List<Product>> SearchProductsAsync(string query, string? marketplace = null);
        Task<List<Deal>> FindDealsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<List<Product>> GetRecentProductsAsync(int count = 10);
    }
}
