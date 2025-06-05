using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Resell_Assistant.Data;
using Resell_Assistant.Models;
using Resell_Assistant.DTOs;
using Resell_Assistant.Services;
using Resell_Assistant.Services.External;
using Xunit;
using System.Threading;

namespace Resell_Assistant.Tests.Services
{
    public class DealDiscoveryServiceTests : IDisposable
    {
        private readonly Mock<IMarketplaceService> _mockMarketplaceService;
        private readonly Mock<IPriceAnalysisService> _mockPriceAnalysisService;
        private readonly Mock<IEbayApiService> _mockEbayApiService;
        private readonly Mock<ILogger<DealDiscoveryService>> _mockLogger;
        private readonly ApplicationDbContext _context;
        private readonly DealDiscoveryService _service;

        public DealDiscoveryServiceTests()
        {
            // Setup in-memory database for testing (should not be used for deals/products)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup mocks
            _mockMarketplaceService = new Mock<IMarketplaceService>();
            _mockPriceAnalysisService = new Mock<IPriceAnalysisService>();
            _mockEbayApiService = new Mock<IEbayApiService>();
            _mockLogger = new Mock<ILogger<DealDiscoveryService>>();

            // Create service instance
            _service = new DealDiscoveryService(
                _context,
                _mockMarketplaceService.Object,
                _mockPriceAnalysisService.Object,
                _mockEbayApiService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task DiscoverIntelligentDealsAsync_ShouldReturnDealsFromLiveAPIs_NotDatabase()
        {
            // Arrange
            var settings = new DealDiscoverySettingsDto
            {
                ExactResultCount = 2,
                UniqueProductCount = 2,
                ListingsPerProduct = 3,
                MinProfitMargin = 10,
                PreferredMarketplaces = new List<string> { "eBay" }
            };

            var mockProducts = new List<Product>
            {
                new Product
                {
                    Id = 1000001, // High ID to simulate in-memory generation
                    Title = "iPhone 15 Pro 128GB",
                    Price = 800,
                    ShippingCost = 0,
                    Marketplace = "eBay",
                    Condition = "New",
                    CreatedAt = DateTime.UtcNow,
                    IsExternalListing = true
                },
                new Product
                {
                    Id = 1000002,
                    Title = "MacBook Air M2",
                    Price = 900,
                    ShippingCost = 0,
                    Marketplace = "eBay",
                    Condition = "Used",
                    CreatedAt = DateTime.UtcNow,
                    IsExternalListing = true
                }
            };

            // Setup marketplace service to return live API data
            _mockMarketplaceService.Setup(m => m.SearchProductsAsync(It.IsAny<string>(), "eBay", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(mockProducts);

            // Act
            var result = await _service.DiscoverIntelligentDealsAsync(settings);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0, "Should return deals from live marketplace APIs");
            
            // Verify all deals have in-memory IDs (not database IDs)
            foreach (var deal in result)
            {
                Assert.True(deal.Id >= 1000000, $"Deal ID {deal.Id} should be in-memory generated (>= 1000000)");
                Assert.True(deal.ProductId >= 2000000, $"Product ID {deal.ProductId} should be in-memory generated (>= 2000000)");
                Assert.NotNull(deal.Product);
                Assert.True(deal.Product.IsExternalListing, "Product should be marked as external listing");
            }

            // Verify marketplace service was called (live API usage)
            _mockMarketplaceService.Verify(m => m.SearchProductsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), 
                Times.AtLeastOnce, "Should call live marketplace APIs");

            // Verify no database operations for products/deals
            Assert.Empty(_context.Products.ToList());
            Assert.Empty(_context.Deals.ToList());
        }

        [Fact]
        public async Task FindTrendingProductsAsync_ShouldReturnProductsFromAPIs_NotDatabase()
        {
            // Arrange
            var mockProducts = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Title = "iPad Pro 12.9",
                    Price = 700,
                    Marketplace = "eBay",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsExternalListing = true
                }
            };

            _mockMarketplaceService.Setup(m => m.SearchProductsAsync(It.IsAny<string>(), "eBay", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(mockProducts);

            // Act
            var result = await _service.FindTrendingProductsAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.All(p => p.IsExternalListing), "All products should be from external APIs");
            
            // Verify marketplace API was called
            _mockMarketplaceService.Verify(m => m.SearchProductsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), 
                Times.AtLeastOnce);

            // Verify no database operations
            Assert.Empty(_context.Products.ToList());
        }

        [Fact]
        public async Task FindListingsForProductAsync_ShouldReturnComparisonListings()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                Title = "Nintendo Switch OLED",
                Price = 300,
                Marketplace = "eBay"
            };

            var mockSimilarProducts = new List<Product>
            {
                new Product
                {
                    Id = 2,
                    Title = "Nintendo Switch OLED Console",
                    Price = 280,
                    ShippingCost = 10,
                    Marketplace = "eBay",
                    Condition = "Used",
                    Url = "https://ebay.com/item/123",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 3,
                    Title = "Nintendo Switch OLED White",
                    Price = 320,
                    ShippingCost = 0,
                    Marketplace = "eBay",
                    Condition = "New",
                    Url = "https://ebay.com/item/456",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockMarketplaceService.Setup(m => m.SearchProductsAsync(It.IsAny<string>(), "eBay", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(mockSimilarProducts);

            // Act
            var result = await _service.FindListingsForProductAsync(product, 5, new List<string> { "eBay" });

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0, "Should return comparison listings");
            Assert.True(result.All(l => !string.IsNullOrEmpty(l.Marketplace)), "All listings should have marketplace");
            Assert.True(result.All(l => l.Price > 0), "All listings should have valid prices");
        }

        [Fact]
        public void CreateDealFromListingsAsync_ShouldCreateInMemoryDeal_WithoutDatabasePersistence()
        {
            // Arrange
            var listings = new List<ComparisonListing>
            {
                new ComparisonListing
                {
                    ProductId = 1,
                    Title = "Test Product A",
                    Price = 100,
                    ShippingCost = 5,
                    Marketplace = "eBay",
                    Condition = "Used",
                    DateListed = DateTime.UtcNow
                },
                new ComparisonListing
                {
                    ProductId = 2,
                    Title = "Test Product B",
                    Price = 150,
                    ShippingCost = 0,
                    Marketplace = "eBay",
                    Condition = "New",
                    DateListed = DateTime.UtcNow
                }
            };

            var settings = new DealDiscoverySettingsDto
            {
                MinProfitMargin = 10,
                PreferredMarketplaces = new List<string> { "eBay" }
            };

            // Act
            var result = _service.CreateDealFromListingsAsync(listings, settings);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id >= 1000000, $"Deal ID {result.Id} should be in-memory generated (>= 1000000)");
            Assert.True(result.ProductId >= 2000000, $"Product ID {result.ProductId} should be in-memory generated (>= 2000000)");
            Assert.NotNull(result.Product);
            Assert.Equal(listings.Count, result.TotalListingsAnalyzed);
            Assert.True(result.PotentialProfit > 0, "Should calculate potential profit");
            Assert.Contains("Intelligent Discovery", result.DiscoveryMethod);

            // Verify no database persistence
            Assert.Empty(_context.Deals.ToList());
            Assert.Empty(_context.Products.ToList());
            Assert.Empty(_context.ComparisonListings.ToList());
        }

        [Fact]
        public async Task ValidateExactResultCountAsync_ShouldValidateBasedOnAPIAvailability_NotDatabase()
        {
            // Arrange
            var mockProducts = new List<Product>
            {
                new Product { Id = 1, Title = "iPhone Test", Price = 500, Marketplace = "eBay" }
            };

            _mockMarketplaceService.Setup(m => m.SearchProductsAsync("iPhone", "eBay", 5, 5))
                .ReturnsAsync(mockProducts);

            // Act
            var result = await _service.ValidateExactResultCountAsync(10);

            // Assert
            Assert.True(result, "Should validate based on API availability");
            
            // Verify API was called for validation
            _mockMarketplaceService.Verify(m => m.SearchProductsAsync("iPhone", "eBay", 5, 5), Times.Once);
        }

        [Fact]
        public void CreateDealFromListingsAsync_ShouldThrowException_WhenNoListingsProvided()
        {
            // Arrange
            var emptyListings = new List<ComparisonListing>();
            var settings = new DealDiscoverySettingsDto();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.CreateDealFromListingsAsync(emptyListings, settings));
        }

        [Fact]
        public async Task DiscoverIntelligentDealsAsync_ShouldGenerateUniqueIds_ForMultipleDeals()
        {
            // Arrange
            var settings = new DealDiscoverySettingsDto
            {
                ExactResultCount = 3,
                UniqueProductCount = 3,
                ListingsPerProduct = 2,
                MinProfitMargin = 5,
                PreferredMarketplaces = new List<string> { "eBay" }
            };

            var mockProducts = Enumerable.Range(1, 6).Select(i => new Product
            {
                Id = i,
                Title = $"Test Product {i}",
                Price = 100 + (i * 10),
                Marketplace = "eBay",
                CreatedAt = DateTime.UtcNow,
                IsExternalListing = true
            }).ToList();

            _mockMarketplaceService.Setup(m => m.SearchProductsAsync(It.IsAny<string>(), "eBay", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(mockProducts);

            // Act
            var result = await _service.DiscoverIntelligentDealsAsync(settings);

            // Assert
            Assert.NotNull(result);
            
            if (result.Count > 1)
            {
                var dealIds = result.Select(d => d.Id).ToList();
                var productIds = result.Select(d => d.ProductId).ToList();
                
                // All deal IDs should be unique
                Assert.Equal(dealIds.Count, dealIds.Distinct().Count());
                
                // All product IDs should be unique
                Assert.Equal(productIds.Count, productIds.Distinct().Count());
                
                // All IDs should be in the expected in-memory ranges
                Assert.True(dealIds.All(id => id >= 1000000), "All deal IDs should be in-memory generated");
                Assert.True(productIds.All(id => id >= 2000000), "All product IDs should be in-memory generated");
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
