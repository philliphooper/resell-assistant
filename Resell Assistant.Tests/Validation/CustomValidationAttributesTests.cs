using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Resell_Assistant.Validation;
using Xunit;

namespace Resell_Assistant.Tests.Validation
{
    public class CustomValidationAttributesTests
    {
        [Theory]
        [InlineData("eBay", true)]
        [InlineData("Amazon", true)]
        [InlineData("Craigslist", true)]
        [InlineData("Unknown", true)]
        [InlineData("NotARealMarketplace", false)]
        public void ValidMarketplaceAttribute_Works(string value, bool expected)
        {
            var attr = new ValidMarketplaceAttribute();
            Assert.Equal(expected, attr.IsValid(value));
        }

        [Theory]
        [InlineData("New", true)]
        [InlineData("Like New", true)]
        [InlineData("Poor", true)]
        [InlineData("NotACondition", false)]
        public void ValidConditionAttribute_Works(string value, bool expected)
        {
            var attr = new ValidConditionAttribute();
            Assert.Equal(expected, attr.IsValid(value));
        }

        [Theory]
        [InlineData(0.01, true)]
        [InlineData(1000, true)]
        [InlineData(0, false)]
        [InlineData(100001, false)]
        [InlineData(10.123, false)]
        public void RealisticPriceAttribute_Works(decimal value, bool expected)
        {
            var attr = new RealisticPriceAttribute(0.01, 100000);
            Assert.Equal(expected, attr.IsValid(value));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public void ValidMarketplaceAttribute_EdgeCases(string value, bool expected)
        {
            var attr = new ValidMarketplaceAttribute();
            Assert.Equal(expected, attr.IsValid(value));
        }

        [Fact]
        public void ValidMarketplaceAttribute_ErrorMessage()
        {
            var attr = new ValidMarketplaceAttribute();
            var msg = attr.FormatErrorMessage("Marketplace");
            Assert.Contains("must be one of", msg);
            Assert.Contains("eBay", msg);
        }

        [Fact]
        public void ValidMarketplaceAttribute_InvalidType_ReturnsFalse()
        {
            var attr = new ValidMarketplaceAttribute();
            Assert.False(attr.IsValid(123));
        }

        private class DummyMarketplaceModel
        {
            [ValidMarketplace]
            public string Marketplace { get; set; }
        }

        [Fact]
        public void ValidMarketplaceAttribute_ModelIntegration()
        {
            var model = new DummyMarketplaceModel { Marketplace = "eBay" };
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, ctx, results, true);
            Assert.True(valid);
        }

        [Fact]
        public void ValidMarketplaceAttribute_ModelIntegration_Fails()
        {
            var model = new DummyMarketplaceModel { Marketplace = "InvalidMarket" };
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, ctx, results, true);
            Assert.False(valid);
            Assert.Contains(results, r => r.ErrorMessage.Contains("must be one of"));
        }

        // Similar tests for ValidConditionAttribute
        [Theory]
        [InlineData(null, true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public void ValidConditionAttribute_EdgeCases(string value, bool expected)
        {
            var attr = new ValidConditionAttribute();
            Assert.Equal(expected, attr.IsValid(value));
        }

        [Fact]
        public void ValidConditionAttribute_ErrorMessage()
        {
            var attr = new ValidConditionAttribute();
            var msg = attr.FormatErrorMessage("Condition");
            Assert.Contains("must be one of", msg);
            Assert.Contains("New", msg);
        }

        [Fact]
        public void ValidConditionAttribute_InvalidType_ReturnsFalse()
        {
            var attr = new ValidConditionAttribute();
            Assert.False(attr.IsValid(123));
        }

        private class DummyConditionModel
        {
            [ValidCondition]
            public string Condition { get; set; }
        }

        [Fact]
        public void ValidConditionAttribute_ModelIntegration()
        {
            var model = new DummyConditionModel { Condition = "New" };
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, ctx, results, true);
            Assert.True(valid);
        }

        [Fact]
        public void ValidConditionAttribute_ModelIntegration_Fails()
        {
            var model = new DummyConditionModel { Condition = "InvalidCond" };
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, ctx, results, true);
            Assert.False(valid);
            Assert.Contains(results, r => r.ErrorMessage.Contains("must be one of"));
        }

        // RealisticPriceAttribute edge and error message tests
        [Theory]
        [InlineData(null, true)]
        [InlineData("not a decimal", false)]
        public void RealisticPriceAttribute_EdgeCases(object value, bool expected)
        {
            var attr = new RealisticPriceAttribute(0.01, 100000);
            Assert.Equal(expected, attr.IsValid(value));
        }

        [Fact]
        public void RealisticPriceAttribute_ErrorMessage()
        {
            var attr = new RealisticPriceAttribute(0.01, 100000);
            var msg = attr.FormatErrorMessage("Price");
            Assert.Contains("must be between", msg);
        }

        private class DummyPriceModel
        {
            [RealisticPrice(0.01, 100000)]
            public decimal Price { get; set; }
        }

        [Fact]
        public void RealisticPriceAttribute_ModelIntegration()
        {
            var model = new DummyPriceModel { Price = 100 };
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, ctx, results, true);
            Assert.True(valid);
        }

        [Fact]
        public void RealisticPriceAttribute_ModelIntegration_Fails()
        {
            var model = new DummyPriceModel { Price = 0 };
            var ctx = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(model, ctx, results, true);
            Assert.False(valid);
            Assert.Contains(results, r => r.ErrorMessage.Contains("must be between"));
        }
    }
}
