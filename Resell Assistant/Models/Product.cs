using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Product title is required")]
        [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
        [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between $0.01 and $999,999.99")]
        public decimal Price { get; set; }
        
        [Range(0, 9999.99, ErrorMessage = "Shipping cost must be between $0 and $9,999.99")]
        public decimal ShippingCost { get; set; }
        
        [Required(ErrorMessage = "Marketplace is required")]
        [MaxLength(100, ErrorMessage = "Marketplace cannot exceed 100 characters")]
        public string Marketplace { get; set; } = string.Empty;
        
        [MaxLength(50, ErrorMessage = "Condition cannot exceed 50 characters")]
        public string? Condition { get; set; }
        
        [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }
        
        [MaxLength(1000, ErrorMessage = "URL cannot exceed 1000 characters")]
        [Url(ErrorMessage = "Please provide a valid URL")]
        public string? Url { get; set; }
          [MaxLength(1000, ErrorMessage = "Image URL cannot exceed 1000 characters")]
        [Url(ErrorMessage = "Please provide a valid image URL")]
        public string? ImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // External API integration properties
        public DateTime? UpdatedAt { get; set; }
        
        [MaxLength(100, ErrorMessage = "External ID cannot exceed 100 characters")]
        public string? ExternalId { get; set; }
        
        public bool IsExternalListing { get; set; } = false;
        
        public DateTime? ExternalUpdatedAt { get; set; }
    }
}
