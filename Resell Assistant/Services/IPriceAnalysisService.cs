using Resell_Assistant.Models;

namespace Resell_Assistant.Services
{
    public interface IPriceAnalysisService
    {
        Task<List<Deal>> AnalyzeDealsAsync();
        Task<Deal?> AnalyzeSingleProductAsync(Product product);
        Task<decimal> CalculateDealScoreAsync(Product product, List<Product> similarProducts);
        Task<decimal> EstimateSellPriceAsync(Product product);
        Task<List<Product>> GetTopDealsAsync(int count = 10);
        Task<decimal> CalculateProfitMarginAsync(decimal buyPrice, decimal sellPrice, decimal fees = 0);
    }
}
