using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class UserPortfolio
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public decimal PurchasePrice { get; set; }
        
        public decimal? SellPrice { get; set; }
        
        [Required]
        public DateTime PurchaseDate { get; set; }
        
        public DateTime? SellDate { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Purchased"; // Purchased, Listed, Sold
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        // Calculated property for profit
        public decimal? Profit => SellPrice.HasValue ? SellPrice.Value - PurchasePrice : null;
        
        // Navigation property
        public virtual Product? Product { get; set; }
    }
}
