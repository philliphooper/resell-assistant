using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class SearchAlert
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string SearchQuery { get; set; } = string.Empty;
        
        public decimal? MinProfit { get; set; }
        
        public decimal? MaxPrice { get; set; }
        
        [MaxLength(100)]
        public string? Marketplace { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastTriggered { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
