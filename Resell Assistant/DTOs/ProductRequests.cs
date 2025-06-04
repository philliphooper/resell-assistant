using System.ComponentModel.DataAnnotations;
using Resell_Assistant.Validation;

namespace Resell_Assistant.DTOs
{
    public class ProductSearchRequest
    {
        [Required(ErrorMessage = "Search query is required")]
        [MinLength(2, ErrorMessage = "Search query must be at least 2 characters")]
        [MaxLength(200, ErrorMessage = "Search query cannot exceed 200 characters")]
        public string Query { get; set; } = string.Empty;
          [MaxLength(100, ErrorMessage = "Marketplace filter cannot exceed 100 characters")]
        [ValidMarketplace]
        public string? Marketplace { get; set; }
        
        [Range(1, 1000, ErrorMessage = "Limit must be between 1 and 1000")]
        public int Limit { get; set; } = 50;
    }

    public class ProductCreateRequest
    {
        [Required(ErrorMessage = "Product title is required")]
        [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
        [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }        [Required(ErrorMessage = "Price is required")]
        [RealisticPrice(0.01, 999999.99)]
        public decimal Price { get; set; }
        
        [RealisticPrice(0, 9999.99)]
        public decimal ShippingCost { get; set; }
        
        [Required(ErrorMessage = "Marketplace is required")]
        [MaxLength(100, ErrorMessage = "Marketplace cannot exceed 100 characters")]
        [ValidMarketplace]
        public string Marketplace { get; set; } = string.Empty;
        
        [MaxLength(50, ErrorMessage = "Condition cannot exceed 50 characters")]
        [ValidCondition]
        public string? Condition { get; set; }
        
        [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }
        
        [MaxLength(1000, ErrorMessage = "URL cannot exceed 1000 characters")]
        [Url(ErrorMessage = "Please provide a valid URL")]
        public string? Url { get; set; }
        
        [MaxLength(1000, ErrorMessage = "Image URL cannot exceed 1000 characters")]
        [Url(ErrorMessage = "Please provide a valid image URL")]
        public string? ImageUrl { get; set; }
    }

    public class ProductAnalyzeRequest
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive number")]
        public int ProductId { get; set; }
    }

    // New DTOs for refactored deal discovery system
    public class DealDiscoverySettingsDto
    {
        [Required(ErrorMessage = "Exact result count is required")]
        [Range(1, 100, ErrorMessage = "Exact result count must be between 1 and 100")]
        public int ExactResultCount { get; set; } = 10;

        [Range(0.01, 10000.00, ErrorMessage = "Target buy price must be between $0.01 and $10,000")]
        public decimal? TargetBuyPrice { get; set; }

        [Required(ErrorMessage = "Unique product count is required")]
        [Range(1, 50, ErrorMessage = "Unique product count must be between 1 and 50")]
        public int UniqueProductCount { get; set; } = 5;

        [Required(ErrorMessage = "Listings per product is required")]
        [Range(1, 20, ErrorMessage = "Listings per product must be between 1 and 20")]
        public int ListingsPerProduct { get; set; } = 5;

        [MaxLength(500, ErrorMessage = "Search terms cannot exceed 500 characters")]
        public string? SearchTerms { get; set; }

        [Range(5, 100, ErrorMessage = "Minimum profit margin must be between 5% and 100%")]
        public decimal MinProfitMargin { get; set; } = 15;

        public List<string> PreferredMarketplaces { get; set; } = new() { "eBay", "Facebook Marketplace" };

        public bool EnableNotifications { get; set; } = true;
    }

    public class ComparisonListingDto
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal ShippingCost { get; set; }
        public string Marketplace { get; set; } = string.Empty;
        public string? Condition { get; set; }
        public string? Location { get; set; }
        public string? Url { get; set; }
        public DateTime DateListed { get; set; }
        public bool IsSelectedDeal { get; set; }
    }

    public class DiscoveryProgressDto
    {
        public string CurrentPhase { get; set; } = string.Empty;
        public string CurrentAction { get; set; } = string.Empty;
        public int ProductsFound { get; set; }
        public int ListingsAnalyzed { get; set; }
        public int DealsCreated { get; set; }
        public int PercentComplete { get; set; }
        public List<string> RecentFindings { get; set; } = new();
    }
}
