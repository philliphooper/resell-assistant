using System.ComponentModel.DataAnnotations;

namespace Resell_Assistant.Validation
{
    /// <summary>
    /// Validates that a marketplace name is from a supported list
    /// </summary>
    public class ValidMarketplaceAttribute : ValidationAttribute
    {
        private static readonly string[] ValidMarketplaces = 
        {
            "eBay", "Amazon", "Facebook Marketplace", "Craigslist", 
            "Mercari", "Poshmark", "Depop", "Vinted", "ThredUp", "Other", "Unknown"
        };

        public override bool IsValid(object? value)
        {
            if (value == null) return true; // Allow null for optional fields
            
            var marketplace = value.ToString();
            return ValidMarketplaces.Contains(marketplace, StringComparer.OrdinalIgnoreCase);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be one of: {string.Join(", ", ValidMarketplaces)}";
        }
    }

    /// <summary>
    /// Validates that a product condition is from a supported list
    /// </summary>
    public class ValidConditionAttribute : ValidationAttribute
    {
        private static readonly string[] ValidConditions = 
        {
            "New", "Like New", "Excellent", "Very Good", "Good", 
            "Acceptable", "Poor", "For Parts", "Unknown"
        };

        public override bool IsValid(object? value)
        {
            if (value == null) return true; // Allow null for optional fields
            
            var condition = value.ToString();
            return ValidConditions.Contains(condition, StringComparer.OrdinalIgnoreCase);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be one of: {string.Join(", ", ValidConditions)}";
        }
    }    /// <summary>
    /// Validates that a price is realistic for resale items
    /// </summary>
    public class RealisticPriceAttribute : ValidationAttribute
    {
        private readonly decimal _minPrice;
        private readonly decimal _maxPrice;

        public RealisticPriceAttribute(double minPrice = 0.01, double maxPrice = 100000)
        {
            _minPrice = (decimal)minPrice;
            _maxPrice = (decimal)maxPrice;
        }

        public override bool IsValid(object? value)
        {
            if (value == null) return true;
            
            if (value is decimal price)
            {
                return price >= _minPrice && price <= _maxPrice && 
                       Math.Round(price, 2) == price; // Ensure max 2 decimal places
            }
            
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be between ${_minPrice:F2} and ${_maxPrice:F2} with at most 2 decimal places";
        }
    }
}
