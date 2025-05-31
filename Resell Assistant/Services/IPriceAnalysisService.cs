using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public interface IPriceAnalysisService
    {
        Task<Deal> AnalyzeProductAsync(Product product);
        Task<List<Deal>> AnalyzeProductsAsync(List<Product> products);
        Task<decimal> EstimateSellingPriceAsync(Product product);
        Task<int> CalculateDealScoreAsync(Product product, decimal estimatedSellPrice);
        Task AddPriceHistoryAsync(int productId, decimal price, string marketplace);
        Task<List<PriceHistory>> GetPriceHistoryAsync(int productId);
    }
}
