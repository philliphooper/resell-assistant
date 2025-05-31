using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Models
{
    public class SearchAlert
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string SearchQuery { get; set; } = string.Empty;
        
        public decimal? MaxPrice { get; set; }
        
        public decimal? MinProfit { get; set; }
        
        public string? Category { get; set; }
        
        public string? Condition { get; set; }
        
        public string? Location { get; set; }
        
        public List<string> Marketplaces { get; set; } = new List<string>();
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime? LastTriggered { get; set; }
        
        public string? EmailNotification { get; set; }
    }
}
