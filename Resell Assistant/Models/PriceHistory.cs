using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class PriceHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Marketplace { get; set; } = string.Empty;
        
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Product? Product { get; set; }
    }
}
