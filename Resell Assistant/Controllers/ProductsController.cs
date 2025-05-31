using Microsoft.AspNetCore.Mvc;
using Resell_Assistant.Services;
using Resell_Assistant.Models;

namespace Resell_Assistant.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IMarketplaceService _marketplaceService;
        private readonly IPriceAnalysisService _priceAnalysisService;

        public ProductsController(IMarketplaceService marketplaceService, IPriceAnalysisService priceAnalysisService)
        {
            _marketplaceService = marketplaceService;
            _priceAnalysisService = priceAnalysisService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<Product>>> SearchProducts(
            [FromQuery] string query,
            [FromQuery] string? marketplace = null)
        {
            try
            {
                var products = await _marketplaceService.SearchProductsAsync(query, marketplace);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest($"Search failed: {ex.Message}");
            }
        }

        [HttpGet("top-deals")]
        public async Task<ActionResult<List<Deal>>> GetTopDeals()
        {
            try
            {
                var topDeals = await _marketplaceService.FindDealsAsync();
                return Ok(topDeals);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get top deals: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            try
            {
                var product = await _marketplaceService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get product: {ex.Message}");
            }
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<Deal>> AnalyzeProduct([FromBody] Product product)
        {
            try
            {
                var deal = await _priceAnalysisService.AnalyzeProductAsync(product);
                return Ok(deal);
            }
            catch (Exception ex)
            {
                return BadRequest($"Analysis failed: {ex.Message}");
            }
        }

        [HttpGet("recent")]
        public async Task<ActionResult<List<Product>>> GetRecentProducts([FromQuery] int count = 10)
        {
            try
            {
                var products = await _marketplaceService.GetRecentProductsAsync(count);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get recent products: {ex.Message}");
            }
        }

        [HttpGet("{id}/price-history")]
        public async Task<ActionResult<List<PriceHistory>>> GetPriceHistory(int id)
        {
            try
            {
                var priceHistory = await _priceAnalysisService.GetPriceHistoryAsync(id);
                return Ok(priceHistory);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get price history: {ex.Message}");
            }
        }
    }
}
