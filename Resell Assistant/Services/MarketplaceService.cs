using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Models;
using RestSharp;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Resell_Assistant.Services
{
    public class MarketplaceService : IMarketplaceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly RestClient _restClient;

        public MarketplaceService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _restClient = new RestClient();
        }

        public async Task<List<Product>> SearchProductsAsync(string query, string marketplace, decimal? maxPrice = null, string? category = null)
        {
            var products = new List<Product>();

            switch (marketplace.ToLower())
            {
                case "ebay":
                    products = await SearchEbayAsync(query, maxPrice, category);
                    break;
                case "facebook":
                    products = await SearchFacebookMarketplaceAsync(query, maxPrice, category);
                    break;
                case "craigslist":
                    products = await SearchCraigslistAsync(query, maxPrice, category);
                    break;
                default:
                    throw new ArgumentException($"Marketplace '{marketplace}' not supported");
            }

            // Save products to database
            foreach (var product in products)
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.ExternalId == product.ExternalId && p.Marketplace == product.Marketplace);

                if (existingProduct == null)
                {
                    _context.Products.Add(product);
                }
                else
                {
                    existingProduct.Price = product.Price;
                    existingProduct.DateUpdated = DateTime.UtcNow;
                    
                    // Add price history
                    _context.PriceHistory.Add(new PriceHistory
                    {
                        ProductId = existingProduct.Id,
                        Price = product.Price,
                        Date = DateTime.UtcNow,
                        Source = marketplace
                    });
                }
            }

            await _context.SaveChangesAsync();
            return products;
        }

        public async Task<List<Product>> SearchAllMarketplacesAsync(string query, decimal? maxPrice = null, string? category = null)
        {
            var allProducts = new List<Product>();
            var marketplaces = new[] { "ebay", "facebook", "craigslist" };

            var tasks = marketplaces.Select(marketplace => 
                SearchProductsAsync(query, marketplace, maxPrice, category));

            var results = await Task.WhenAll(tasks);
            
            foreach (var products in results)
            {
                allProducts.AddRange(products);
            }

            return allProducts.OrderBy(p => p.Price).ToList();
        }

        private async Task<List<Product>> SearchEbayAsync(string query, decimal? maxPrice, string? category)
        {
            var products = new List<Product>();
            
            try
            {
                // Note: In production, you would use the official eBay API
                // This is a simplified web scraping approach for demonstration
                var searchUrl = $"https://www.ebay.com/sch/i.html?_nkw={Uri.EscapeDataString(query)}";
                if (maxPrice.HasValue)
                {
                    searchUrl += $"&_udhi={maxPrice.Value}";
                }

                var request = new RestRequest(searchUrl);
                var response = await _restClient.ExecuteAsync(request);

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(response.Content);

                    var itemNodes = doc.DocumentNode.SelectNodes("//div[@class='s-item__wrapper clearfix']");
                    
                    if (itemNodes != null)
                    {
                        foreach (var node in itemNodes.Take(20)) // Limit to 20 results
                        {
                            try
                            {
                                var titleNode = node.SelectSingleNode(".//h3[@class='s-item__title']/a");
                                var priceNode = node.SelectSingleNode(".//span[@class='s-item__price']");
                                var imageNode = node.SelectSingleNode(".//img[@class='s-item__image']");
                                var linkNode = node.SelectSingleNode(".//h3[@class='s-item__title']/a");

                                if (titleNode != null && priceNode != null)
                                {
                                    var title = titleNode.InnerText?.Trim();
                                    var priceText = priceNode.InnerText?.Trim();
                                    var link = linkNode?.GetAttributeValue("href", "");
                                    var imageUrl = imageNode?.GetAttributeValue("src", "");

                                    if (decimal.TryParse(priceText?.Replace("$", "").Replace(",", ""), out var price))
                                    {
                                        var product = new Product
                                        {
                                            Title = title ?? "",
                                            Price = price,
                                            Marketplace = "eBay",
                                            ExternalId = ExtractEbayItemId(link),
                                            ProductUrl = link,
                                            ImageUrl = imageUrl,
                                            DateListed = DateTime.UtcNow,
                                            DateUpdated = DateTime.UtcNow
                                        };

                                        products.Add(product);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log error and continue with next item
                                Console.WriteLine($"Error parsing eBay item: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching eBay: {ex.Message}");
            }

            return products;
        }

        private async Task<List<Product>> SearchFacebookMarketplaceAsync(string query, decimal? maxPrice, string? category)
        {
            // Note: Facebook Marketplace doesn't have a public API
            // This would require web scraping or alternative approaches
            // For now, return empty list
            return new List<Product>();
        }

        private async Task<List<Product>> SearchCraigslistAsync(string query, decimal? maxPrice, string? category)
        {
            var products = new List<Product>();
            
            try
            {
                // Simple Craigslist search - in production you'd want to handle multiple cities
                var searchUrl = $"https://seattle.craigslist.org/search/sss?query={Uri.EscapeDataString(query)}";
                if (maxPrice.HasValue)
                {
                    searchUrl += $"&max_price={maxPrice.Value}";
                }

                var request = new RestRequest(searchUrl);
                var response = await _restClient.ExecuteAsync(request);

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(response.Content);

                    var itemNodes = doc.DocumentNode.SelectNodes("//li[@class='result-row']");
                    
                    if (itemNodes != null)
                    {
                        foreach (var node in itemNodes.Take(20))
                        {
                            try
                            {
                                var titleNode = node.SelectSingleNode(".//a[@class='result-title hdrlnk']");
                                var priceNode = node.SelectSingleNode(".//span[@class='result-price']");
                                var dateNode = node.SelectSingleNode(".//time[@class='result-date']");

                                if (titleNode != null && priceNode != null)
                                {
                                    var title = titleNode.InnerText?.Trim();
                                    var priceText = priceNode.InnerText?.Trim();
                                    var link = titleNode.GetAttributeValue("href", "");
                                    var dateText = dateNode?.GetAttributeValue("datetime", "");

                                    if (decimal.TryParse(priceText?.Replace("$", "").Replace(",", ""), out var price))
                                    {
                                        var product = new Product
                                        {
                                            Title = title ?? "",
                                            Price = price,
                                            Marketplace = "Craigslist",
                                            ExternalId = ExtractCraigslistItemId(link),
                                            ProductUrl = link,
                                            DateListed = DateTime.TryParse(dateText, out var date) ? date : DateTime.UtcNow,
                                            DateUpdated = DateTime.UtcNow
                                        };

                                        products.Add(product);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing Craigslist item: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching Craigslist: {ex.Message}");
            }

            return products;
        }

        public async Task<Product?> GetProductDetailsAsync(string externalId, string marketplace)
        {
            return await _context.Products
                .Include(p => p.PriceHistory)
                .FirstOrDefaultAsync(p => p.ExternalId == externalId && p.Marketplace == marketplace);
        }

        public async Task<List<Product>> GetSimilarProductsAsync(Product product)
        {
            var keywords = product.Title.Split(' ').Where(w => w.Length > 3).Take(3);
            var searchQuery = string.Join(" ", keywords);

            return await SearchAllMarketplacesAsync(searchQuery, product.Price * 1.5m, product.Category);
        }

        public async Task<decimal?> GetAveragePriceAsync(string title, string category)
        {
            var similarProducts = await _context.Products
                .Where(p => p.Title.Contains(title) || (category != null && p.Category == category))
                .Where(p => p.DateUpdated > DateTime.UtcNow.AddDays(-30)) // Only recent data
                .ToListAsync();

            return similarProducts.Any() ? similarProducts.Average(p => p.Price) : null;
        }

        public async Task UpdateProductPricesAsync()
        {
            var activeProducts = await _context.Products
                .Where(p => p.IsActive && p.DateUpdated < DateTime.UtcNow.AddHours(-6))
                .ToListAsync();

            foreach (var product in activeProducts)
            {
                try
                {
                    // Re-scrape the product to get updated price
                    var updatedProducts = await SearchProductsAsync(product.Title, product.Marketplace);
                    var updatedProduct = updatedProducts.FirstOrDefault(p => p.ExternalId == product.ExternalId);

                    if (updatedProduct != null && updatedProduct.Price != product.Price)
                    {
                        // Add price history entry
                        _context.PriceHistory.Add(new PriceHistory
                        {
                            ProductId = product.Id,
                            Price = updatedProduct.Price,
                            Date = DateTime.UtcNow,
                            Source = product.Marketplace
                        });

                        product.Price = updatedProduct.Price;
                        product.DateUpdated = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating product {product.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private string ExtractEbayItemId(string url)
        {
            if (string.IsNullOrEmpty(url)) return Guid.NewGuid().ToString();
            
            var match = System.Text.RegularExpressions.Regex.Match(url, @"/itm/(\d+)");
            return match.Success ? match.Groups[1].Value : Guid.NewGuid().ToString();
        }

        private string ExtractCraigslistItemId(string url)
        {
            if (string.IsNullOrEmpty(url)) return Guid.NewGuid().ToString();
            
            var match = System.Text.RegularExpressions.Regex.Match(url, @"(\d+)\.html");
            return match.Success ? match.Groups[1].Value : Guid.NewGuid().ToString();
        }
    }
}
