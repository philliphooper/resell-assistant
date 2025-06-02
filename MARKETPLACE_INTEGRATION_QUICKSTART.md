# Quick Start Guide: Implementing Real Marketplace Data

## 🚀 Immediate Action Items (Get Started Today)

### 1. **Set Up eBay Developer Account** (30 minutes)
1. Go to https://developer.ebay.com/
2. Create account and get sandbox credentials
3. Navigate to "My Account" → "Keys"
4. Get your Client ID and Client Secret

### 2. **Update Configuration** (5 minutes)
Replace the placeholder values in `appsettings.json`:
```json
{
  "ApiKeys": {
    "EbayClientId": "YourActualEbayClientId",
    "EbayClientSecret": "YourActualEbayClientSecret"
  },
  "ExternalApis": {
    "Ebay": {
      "Environment": "SANDBOX",
      "BaseUrl": "https://api.sandbox.ebay.com",
      "RateLimitPerSecond": 5
    }
  }
}
```

### 3. **Install Required NuGet Packages** (5 minutes)
```bash
cd "Resell Assistant"
dotnet add package Microsoft.Extensions.Http
dotnet add package System.Text.Json
dotnet add package Microsoft.Extensions.Http.Polly
```

### 4. **Create Basic eBay Service** (2 hours)

#### 4.1 Create the service interface:
```csharp
// Services/External/IEbayApiService.cs
using Resell_Assistant.Models;

namespace Resell_Assistant.Services.External
{
    public interface IEbayApiService
    {
        Task<List<Product>> SearchProductsAsync(string query, int limit = 20);
        Task<string> GetAccessTokenAsync();
    }
}
```

#### 4.2 Implement the basic service:
```csharp
// Services/External/EbayApiService.cs
using System.Text.Json;
using Resell_Assistant.Models;
using System.Text;

namespace Resell_Assistant.Services.External
{
    public class EbayApiService : IEbayApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EbayApiService> _logger;
        private string? _cachedToken;
        private DateTime _tokenExpiry;

        public EbayApiService(HttpClient httpClient, IConfiguration configuration, ILogger<EbayApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            var clientId = _configuration["ApiKeys:EbayClientId"];
            var clientSecret = _configuration["ApiKeys:EbayClientSecret"];
            var baseUrl = _configuration["ExternalApis:Ebay:BaseUrl"];

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/identity/v1/oauth2/token");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "https://api.ebay.com/oauth/api_scope")
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

            _cachedToken = tokenResponse.access_token;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60); // Refresh 1 min early

            return _cachedToken;
        }

        public async Task<List<Product>> SearchProductsAsync(string query, int limit = 20)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                var baseUrl = _configuration["ExternalApis:Ebay:BaseUrl"];

                var request = new HttpRequestMessage(HttpMethod.Get, 
                    $"{baseUrl}/buy/browse/v1/item_summary/search?q={Uri.EscapeDataString(query)}&limit={limit}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"eBay API returned {response.StatusCode} for query: {query}");
                    return new List<Product>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<EbaySearchResponse>(content);

                return ConvertToProducts(searchResponse?.itemSummaries ?? new List<EbayItem>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching eBay for query: {query}");
                return new List<Product>();
            }
        }

        private List<Product> ConvertToProducts(List<EbayItem> ebayItems)
        {
            return ebayItems.Select(item => new Product
            {
                Title = item.title ?? "Unknown",
                Description = item.shortDescription ?? "",
                Price = decimal.TryParse(item.price?.value, out var price) ? price : 0,
                Marketplace = "eBay",
                Condition = item.condition ?? "Unknown",
                Location = item.itemLocation?.city ?? "Unknown",
                Url = item.itemWebUrl ?? "",
                ImageUrl = item.image?.imageUrl ?? "",
                ExternalId = item.itemId ?? "",
                IsExternalListing = true,
                ExternalUpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }
    }

    // DTO classes for eBay API responses
    public class TokenResponse
    {
        public string access_token { get; set; } = "";
        public int expires_in { get; set; }
    }

    public class EbaySearchResponse
    {
        public List<EbayItem> itemSummaries { get; set; } = new();
    }

    public class EbayItem
    {
        public string? itemId { get; set; }
        public string? title { get; set; }
        public string? shortDescription { get; set; }
        public EbayPrice? price { get; set; }
        public string? condition { get; set; }
        public EbayLocation? itemLocation { get; set; }
        public string? itemWebUrl { get; set; }
        public EbayImage? image { get; set; }
    }

    public class EbayPrice
    {
        public string? value { get; set; }
        public string? currency { get; set; }
    }

    public class EbayLocation
    {
        public string? city { get; set; }
        public string? stateOrProvince { get; set; }
        public string? country { get; set; }
    }

    public class EbayImage
    {
        public string? imageUrl { get; set; }
    }
}
```

### 5. **Update Product Model** (15 minutes)
Add these properties to your existing `Product.cs`:
```csharp
// Models/Product.cs additions
public string? ExternalId { get; set; } // eBay item ID
public bool IsExternalListing { get; set; } = false; // True for API data
public DateTime? ExternalUpdatedAt { get; set; } // Last sync from external API
```

### 6. **Register Services in Program.cs** (5 minutes)
```csharp
// Program.cs additions
builder.Services.AddHttpClient<IEbayApiService, EbayApiService>();
builder.Services.AddScoped<IEbayApiService, EbayApiService>();
```

### 7. **Update MarketplaceService** (30 minutes)
Modify your existing `MarketplaceService.cs`:
```csharp
public class MarketplaceService : IMarketplaceService
{
    private readonly ApplicationDbContext _context;
    private readonly IEbayApiService _ebayApiService;

    public MarketplaceService(ApplicationDbContext context, IEbayApiService ebayApiService)
    {
        _context = context;
        _ebayApiService = ebayApiService;
    }

    public async Task<List<Product>> SearchProductsAsync(string query, string? marketplace = null)
    {
        var allProducts = new List<Product>();

        // Search local database (existing functionality)
        var localProducts = await SearchLocalProductsAsync(query, marketplace);
        allProducts.AddRange(localProducts);

        // Search eBay if no marketplace filter or specifically eBay
        if (string.IsNullOrEmpty(marketplace) || marketplace.Equals("eBay", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var ebayProducts = await _ebayApiService.SearchProductsAsync(query, 20);
                allProducts.AddRange(ebayProducts);
            }
            catch (Exception ex)
            {
                // Log error but continue with local results
                Console.WriteLine($"eBay search failed: {ex.Message}");
            }
        }

        return allProducts
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToList();
    }

    private async Task<List<Product>> SearchLocalProductsAsync(string query, string? marketplace)
    {
        var productsQuery = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            productsQuery = productsQuery.Where(p => p.Title.Contains(query) || 
                                                    (p.Description != null && p.Description.Contains(query)));
        }

        if (!string.IsNullOrEmpty(marketplace))
        {
            productsQuery = productsQuery.Where(p => p.Marketplace == marketplace);
        }

        return await productsQuery
            .Where(p => !p.IsExternalListing) // Only local products
            .OrderByDescending(p => p.CreatedAt)
            .Take(25)
            .ToListAsync();
    }

    // ... existing methods remain the same
}
```

### 8. **Database Migration** (10 minutes)
```bash
cd "Resell Assistant"
dotnet ef migrations add AddExternalListingFields
dotnet ef database update
```

### 9. **Test the Integration** (15 minutes)
1. Start the application: `dotnet run`
2. Navigate to https://localhost:5001/swagger
3. Test the `/api/products/search` endpoint with a query like "iPhone"
4. You should now see both local AND eBay results!

## 🎯 Expected Results After Implementation

### Before (Current State):
- Search returns only 8 sample products from local database
- All data is static placeholder information
- No real marketplace pricing or trends

### After (With eBay Integration):
- Search returns local products PLUS real eBay listings
- Live pricing data from actual eBay marketplace
- Real product descriptions and images
- Actual marketplace URLs for purchase links

### Sample Search Result Mix:
```json
{
  "results": [
    {
      "title": "iPhone 13 Pro 128GB - Real eBay Listing",
      "price": 750.00,
      "marketplace": "eBay",
      "isExternalListing": true,
      "url": "https://www.ebay.com/itm/123456789",
      "externalId": "123456789"
    },
    {
      "title": "iPhone 13 Pro 128GB Unlocked",
      "price": 650.00,
      "marketplace": "eBay", 
      "isExternalListing": false,
      "url": "https://example.com/iphone13pro"
    }
  ]
}
```

## 🔍 Troubleshooting Common Issues

### Issue 1: "Unauthorized" Error
**Solution**: Check your eBay Client ID and Secret are correct in `appsettings.json`

### Issue 2: No eBay Results Appearing
**Solutions**:
- Check logs for API errors
- Verify sandbox environment is working
- Test token generation independently

### Issue 3: Rate Limiting Errors
**Solutions**:
- Implement delay between requests
- Cache results for repeated searches
- Use eBay's rate limiting headers

## 📈 Next Steps After Basic Implementation

### Immediate Improvements (Week 1):
1. **Add Rate Limiting**: Prevent API abuse
2. **Implement Caching**: Cache eBay results for 15 minutes
3. **Better Error Handling**: Graceful degradation when APIs fail
4. **Add More Search Filters**: Price range, condition, location

### Phase 2 (Week 2):
1. **Facebook Marketplace Scraping**: Add second data source
2. **Price History Tracking**: Store historical pricing data
3. **Background Sync**: Automatically update popular searches

### Phase 3 (Week 3-4):
1. **Deal Detection**: Compare prices across sources automatically
2. **Alert System**: Notify when good deals are found
3. **Advanced Analytics**: Market trend analysis

## 💡 Pro Tips

1. **Start Small**: Get eBay working first before adding other sources
2. **Test Thoroughly**: Use eBay's sandbox environment extensively  
3. **Monitor Usage**: Watch your API call limits carefully
4. **Cache Aggressively**: Reduce API calls with smart caching
5. **Handle Failures Gracefully**: Always fall back to local data when external APIs fail

---

**Ready to transform your app from demo to production? Start with step 1 above! 🚀**

After implementing these steps, your Resell Assistant will finally have REAL marketplace data and become a truly functional reselling tool instead of just a demonstration application.
