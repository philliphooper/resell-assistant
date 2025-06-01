using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Resell_Assistant.Models;

namespace Resell_Assistant.Services.External;

/// <summary>
/// Basic Facebook Marketplace scraping service (stub implementation)
/// </summary>
public class FacebookMarketplaceService : IFacebookMarketplaceService
{
    private readonly ILogger<FacebookMarketplaceService> _logger;

    public FacebookMarketplaceService(ILogger<FacebookMarketplaceService> logger)
    {
        _logger = logger;
    }

    public async Task<List<Product>> SearchProductsAsync(string query, string? location = null, int limit = 50)
    {
        // NOTE: This is a stub. Real implementation would fetch and parse HTML from Facebook Marketplace.
        _logger.LogInformation("[Stub] Facebook Marketplace search for query: {Query}, location: {Location}, limit: {Limit}", query, location, limit);
        await Task.Delay(100); // Simulate async work
        return new List<Product>();
    }
}
