using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public interface IMarketplaceService
    {
        Task<List<Product>> SearchProductsAsync(string query, string marketplace, decimal? maxPrice = null, string? category = null);
        Task<List<Product>> SearchAllMarketplacesAsync(string query, decimal? maxPrice = null, string? category = null);
        Task<Product?> GetProductDetailsAsync(string externalId, string marketplace);
        Task<List<Product>> GetSimilarProductsAsync(Product product);
        Task<decimal?> GetAveragePriceAsync(string title, string category);
        Task UpdateProductPricesAsync();
    }
}
