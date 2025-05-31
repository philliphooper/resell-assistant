namespace Resell_Assistant.Models
{
    public class Deal
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        public decimal PotentialProfit { get; set; }
        
        public decimal Score { get; set; } // Algorithm-calculated deal score (0-100)
        
        public string? Reasoning { get; set; } // Why this is considered a good deal
        
        public DateTime IdentifiedDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? MarketComparison { get; set; } // JSON data of compared prices
        
        public int DaysOnMarket { get; set; }
        
        public decimal? EstimatedSellPrice { get; set; }
        
        public decimal? ConfidenceLevel { get; set; } // 0-100 confidence in the deal
        
        // Navigation properties
        public Product Product { get; set; } = null!;
    }
}
