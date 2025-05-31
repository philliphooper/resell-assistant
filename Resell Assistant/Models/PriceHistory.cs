namespace Resell_Assistant.Models
{
    public class PriceHistory
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        public decimal Price { get; set; }
        
        public DateTime Date { get; set; }
        
        public string? Source { get; set; } // Which marketplace or scraper
        
        // Navigation properties
        public Product Product { get; set; } = null!;
    }
}
