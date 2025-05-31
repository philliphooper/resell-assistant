using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class Deal
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public decimal PotentialProfit { get; set; }
        
        [Required]
        public decimal EstimatedSellPrice { get; set; }
        
        [Required]
        [Range(0, 100)]
        public int DealScore { get; set; }
        
        [Required]
        [Range(0, 100)]
        public int Confidence { get; set; }
        
        [MaxLength(1000)]
        public string? Reasoning { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Product? Product { get; set; }
    }
}
