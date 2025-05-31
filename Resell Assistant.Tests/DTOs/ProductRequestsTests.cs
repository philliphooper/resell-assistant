using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Resell_Assistant.DTOs;
using Xunit;

namespace Resell_Assistant.Tests.DTOs
{
    public class ProductRequestsTests
    {
        [Fact]
        public void ProductSearchRequest_Validation_Fails_On_EmptyQuery()
        {
            var dto = new ProductSearchRequest { Query = "" };
            var results = ValidateModel(dto);
            Assert.Contains(results, r => r.ErrorMessage.Contains("Search query is required"));
        }

        [Fact]
        public void ProductCreateRequest_Validation_Fails_On_InvalidMarketplace()
        {
            var dto = new ProductCreateRequest
            {
                Title = "Test Product",
                Price = 10,
                Marketplace = "NotARealMarketplace"
            };
            var results = ValidateModel(dto);
            Assert.Contains(results, r => r.ErrorMessage.Contains("must be one of"));
        }

        [Fact]
        public void ProductCreateRequest_Validation_Fails_On_InvalidPrice()
        {
            var dto = new ProductCreateRequest
            {
                Title = "Test Product",
                Price = 0,
                Marketplace = "eBay"
            };
            var results = ValidateModel(dto);
            Assert.Contains(results, r => r.ErrorMessage.Contains("must be between $0.01"));
        }

        private static IList<ValidationResult> ValidateModel(object model)
        {
            var context = new ValidationContext(model, null, null);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }
    }
}
