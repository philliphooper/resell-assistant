using Microsoft.AspNetCore.Mvc;
using Resell_Assistant.Services;
using Resell_Assistant.Models;
using Resell_Assistant.DTOs;
using Resell_Assistant.Filters;

namespace Resell_Assistant.Controllers
{    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IMarketplaceService _marketplaceService;
        private readonly IPriceAnalysisService _priceAnalysisService;

        public ProductsController(IMarketplaceService marketplaceService, IPriceAnalysisService priceAnalysisService)
        {
            _marketplaceService = marketplaceService;
            _priceAnalysisService = priceAnalysisService;
        }        [HttpGet("search")]
        public async Task<ActionResult<List<Product>>> SearchProducts([FromQuery] ProductSearchRequest request)
        {
            var products = await _marketplaceService.SearchProductsAsync(request.Query, request.Marketplace);
            return Ok(products);
        }        [HttpGet("top-deals")]
        public async Task<ActionResult<List<Deal>>> GetTopDeals()
        {
            var topDeals = await _marketplaceService.FindDealsAsync();
            return Ok(topDeals);
        }        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Product ID must be a positive number");
            }
            
            var product = await _marketplaceService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }        [HttpPost("analyze")]
        public async Task<ActionResult<Deal>> AnalyzeProduct([FromBody] ProductAnalyzeRequest request)
        {
            var product = await _marketplaceService.GetProductByIdAsync(request.ProductId);
            if (product == null)
            {
                return NotFound("Product not found");
            }
            
            var deal = await _priceAnalysisService.AnalyzeProductAsync(product);
            return Ok(deal);
        }        [HttpGet("recent")]
        public async Task<ActionResult<List<Product>>> GetRecentProducts([FromQuery] int count = 10)
        {
            if (count <= 0 || count > 1000)
            {
                return BadRequest("Count must be between 1 and 1000");
            }
            
            var products = await _marketplaceService.GetRecentProductsAsync(count);
            return Ok(products);
        }        [HttpGet("{id}/price-history")]
        public async Task<ActionResult<List<PriceHistory>>> GetPriceHistory(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Product ID must be a positive number");
            }
            
            var priceHistory = await _priceAnalysisService.GetPriceHistoryAsync(id);
            return Ok(priceHistory);
        }
    }
}
