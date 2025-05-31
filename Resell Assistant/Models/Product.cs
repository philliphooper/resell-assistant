using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        public decimal ShippingCost { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Marketplace { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? Condition { get; set; }
        
        [MaxLength(200)]
        public string? Location { get; set; }
        
        [MaxLength(1000)]
        public string? Url { get; set; }
        
        [MaxLength(1000)]
        public string? ImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
