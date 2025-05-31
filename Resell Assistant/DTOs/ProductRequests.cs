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
}
