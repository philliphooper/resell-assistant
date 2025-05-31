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
        public string ExternalId { get; set; } = string.Empty;
        
        [Required]
        public string Marketplace { get; set; } = string.Empty; // eBay, Amazon, Facebook, etc.
        
        public decimal Price { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public string? ProductUrl { get; set; }
        
        public string? Category { get; set; }
        
        public string? Condition { get; set; }
        
        public string? Location { get; set; }
        
        public string? Seller { get; set; }
        
        public int? ViewCount { get; set; }
        
        public int? WatchCount { get; set; }
        
        public DateTime DateListed { get; set; }
        
        public DateTime DateUpdated { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<PriceHistory> PriceHistory { get; set; } = new List<PriceHistory>();
    }
}
