using Microsoft.AspNetCore.Mvc;
using Resell_Assistant.Services.External;
using Resell_Assistant.Models;

namespace Resell_Assistant.Controllers;

/*
// Facebook Marketplace controller temporarily disabled
// TODO: Re-enable when proper implementation is complete

[ApiController]
[Route("api/[controller]")]
public class FacebookMarketplaceController : ControllerBase
{
    private readonly IFacebookMarketplaceService _facebookService;

    public FacebookMarketplaceController(IFacebookMarketplaceService facebookService)
    {
        _facebookService = facebookService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Product>>> Search(string query, string? location = null, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Query is required." });
        var results = await _facebookService.SearchProductsAsync(query, location, limit);
        return Ok(results);
    }
}
*/
