using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Resell_Assistant.Controllers;
using Resell_Assistant.DTOs;
using Resell_Assistant.Models;
using Resell_Assistant.Services;
using Xunit;

namespace Resell_Assistant.Tests.Controllers
{
    public class ProductsControllerValidationTests
    {
        [Fact]
        public async Task SearchProducts_Returns_Ok_When_ModelState_Invalid_But_FilterNotApplied()
        {
            // In direct controller unit tests, global filters like ValidateModelState are not triggered.
            // So the controller action will execute and return Ok, not BadRequest.
            var marketplaceService = new Mock<IMarketplaceService>();
            var priceAnalysisService = new Mock<IPriceAnalysisService>();
            var controller = new ProductsController(marketplaceService.Object, priceAnalysisService.Object);
            controller.ModelState.AddModelError("Query", "Search query is required");

            var result = await controller.SearchProducts(new ProductSearchRequest { Query = "" });
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeProduct_Returns_NotFound_If_Product_Does_Not_Exist()
        {
            var marketplaceService = new Mock<IMarketplaceService>();
            var priceAnalysisService = new Mock<IPriceAnalysisService>();
            marketplaceService.Setup(m => m.GetProductByIdAsync(It.IsAny<int>())).ReturnsAsync((Product)null);
            var controller = new ProductsController(marketplaceService.Object, priceAnalysisService.Object);

            var result = await controller.AnalyzeProduct(new ProductAnalyzeRequest { ProductId = 999 });
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
    }
}
