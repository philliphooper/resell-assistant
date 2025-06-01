namespace Resell_Assistant.Services.External;

using Resell_Assistant.Models;

/// <summary>
/// Interface for Facebook Marketplace integration service
/// </summary>
public interface IFacebookMarketplaceService
{
    /// <summary>
    /// Search for products on Facebook Marketplace
    /// </summary>
    Task<List<Product>> SearchProductsAsync(string query, string? location = null, int limit = 50);
}
