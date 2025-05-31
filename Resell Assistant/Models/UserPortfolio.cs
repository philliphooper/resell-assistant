namespace Resell_Assistant.Models
{
    public class UserPortfolio
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        public decimal PurchasePrice { get; set; }
        
        public DateTime PurchaseDate { get; set; }
        
        public decimal? SellPrice { get; set; }
        
        public DateTime? SellDate { get; set; }
        
        public decimal? Profit { get; set; }
        
        public string Status { get; set; } = "Purchased"; // Purchased, Listed, Sold
        
        public string? Notes { get; set; }
        
        public string? PurchaseLocation { get; set; }
        
        public string? SellLocation { get; set; }
        
        public decimal? ShippingCost { get; set; }
        
        public decimal? SellingFees { get; set; }
        
        public int? DaysToSell { get; set; }
        
        // Navigation properties
        public Product Product { get; set; } = null!;
    }
}
