using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class Deal
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive number")]
        public int ProductId { get; set; }
        
        [Required(ErrorMessage = "Potential profit is required")]
        [Range(-99999.99, 99999.99, ErrorMessage = "Potential profit must be between -$99,999.99 and $99,999.99")]
        public decimal PotentialProfit { get; set; }
        
        [Required(ErrorMessage = "Estimated sell price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Estimated sell price must be between $0.01 and $999,999.99")]
        public decimal EstimatedSellPrice { get; set; }
        
        [Required(ErrorMessage = "Deal score is required")]
        [Range(0, 100, ErrorMessage = "Deal score must be between 0 and 100")]
        public int DealScore { get; set; }
        
        [Required(ErrorMessage = "Confidence is required")]
        [Range(0, 100, ErrorMessage = "Confidence must be between 0 and 100")]
        public int Confidence { get; set; }
        
        [MaxLength(1000, ErrorMessage = "Reasoning cannot exceed 1000 characters")]
        public string? Reasoning { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Product? Product { get; set; }
    }
}
