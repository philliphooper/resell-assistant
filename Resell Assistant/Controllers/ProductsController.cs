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
            [FromQuery] string? marketplace = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? category = null)
        {
            try
            {
                List<Product> products;
                
                if (string.IsNullOrEmpty(marketplace))
                {
                    products = await _marketplaceService.SearchAllMarketplacesAsync(query, maxPrice, category);
                }
                else
                {
                    products = await _marketplaceService.SearchProductsAsync(query, marketplace, maxPrice, category);
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest($"Search failed: {ex.Message}");
            }
        }

        [HttpGet("top-deals")]
        public async Task<ActionResult<List<Product>>> GetTopDeals([FromQuery] int count = 10)
        {
            try
            {
                var topDeals = await _priceAnalysisService.GetTopDealsAsync(count);
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
                // This would need to be implemented in the marketplace service
                // For now, return NotFound
                return NotFound();
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
                var deal = await _priceAnalysisService.AnalyzeSingleProductAsync(product);
                if (deal == null)
                {
                    return BadRequest("Product does not appear to be a good deal");
                }
                return Ok(deal);
            }
            catch (Exception ex)
            {
                return BadRequest($"Analysis failed: {ex.Message}");
            }
        }

        [HttpGet("similar/{productId}")]
        public async Task<ActionResult<List<Product>>> GetSimilarProducts(int productId)
        {
            try
            {
                // Would need to get product first, then find similar ones
                // Implementation would go here
                return Ok(new List<Product>());
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get similar products: {ex.Message}");
            }
        }
    }
}
