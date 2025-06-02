using Microsoft.AspNetCore.Mvc;
using Resell_Assistant.Models.Configuration;
using Resell_Assistant.Services;
using Resell_Assistant.Services.External;

namespace Resell_Assistant.Controllers
{
    [ApiController]
    [Route("api/[controller]")]    public class SettingsController : ControllerBase
    {
        private readonly ICredentialService _credentialService;
        private readonly IEbayApiService _ebayApiService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ICredentialService credentialService, 
            IEbayApiService ebayApiService,
            ILogger<SettingsController> logger)
        {
            _credentialService = credentialService;
            _ebayApiService = ebayApiService;
            _logger = logger;
        }

        /// <summary>
        /// Get the status of API credentials for all services
        /// </summary>
        [HttpGet("credentials/status")]
        public async Task<ActionResult<object>> GetCredentialStatus()
        {
            try
            {
                var ebayStatus = await _credentialService.GetCredentialStatusAsync("eBay");
                
                return Ok(new
                {
                    eBay = ebayStatus,
                    // Future: Add other services here (Facebook, etc.)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credential status");
                return StatusCode(500, new { message = "Failed to retrieve credential status", details = ex.Message });
            }
        }

        /// <summary>
        /// Save eBay API credentials
        /// </summary>
        [HttpPost("credentials/ebay")]
        public async Task<ActionResult> SaveEbayCredentials([FromBody] ApiCredentialsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate that it's specifically for eBay
                if (request.Service.ToLower() != "ebay")
                {
                    return BadRequest(new { message = "Invalid service. This endpoint is for eBay credentials only." });
                }

                // Validate environment
                if (request.Environment != "sandbox" && request.Environment != "production")
                {
                    return BadRequest(new { message = "Environment must be either 'sandbox' or 'production'." });
                }

                var success = await _credentialService.SaveCredentialsAsync(
                    "eBay",
                    request.ClientId,
                    request.ClientSecret,
                    request.Environment);

                if (success)
                {
                    _logger.LogInformation("eBay credentials saved successfully for environment: {Environment}", request.Environment);
                    return Ok(new { message = "eBay credentials saved successfully", environment = request.Environment });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to save eBay credentials" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save eBay credentials");
                return StatusCode(500, new { message = "Failed to save credentials", details = ex.Message });
            }
        }        /// <summary>
        /// Test eBay API credentials by making a test call
        /// </summary>
        [HttpPost("credentials/ebay/test")]
        public async Task<ActionResult> TestEbayCredentials()
        {
            try
            {
                var hasCredentials = await _credentialService.HasCredentialsAsync("eBay");
                if (!hasCredentials)
                {
                    return BadRequest(new { message = "No eBay credentials configured. Please set up your credentials first." });
                }

                _logger.LogInformation("Starting eBay API connection test...");

                // Test actual eBay API connection
                var isConnected = await _ebayApiService.TestConnectionAsync();
                
                if (isConnected)
                {
                    _logger.LogInformation("eBay API connection test successful");
                    return Ok(new { 
                        message = "eBay credentials are valid and eBay API is accessible",
                        isConnected = true,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("eBay API connection test failed");
                    return BadRequest(new { 
                        message = "eBay credentials are configured but connection test failed. Please check your credentials and ensure they are valid for the selected environment.",
                        isConnected = false,
                        details = "Check application logs for detailed error information."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test eBay credentials: {Message}", ex.Message);
                return StatusCode(500, new { 
                    message = "Failed to test credentials", 
                    details = ex.Message,
                    isConnected = false
                });
            }
        }

        /// <summary>
        /// Delete eBay API credentials
        /// </summary>
        [HttpDelete("credentials/ebay")]
        public async Task<ActionResult> DeleteEbayCredentials()
        {
            try
            {
                var success = await _credentialService.DeleteCredentialsAsync("eBay");
                
                if (success)
                {
                    _logger.LogInformation("eBay credentials deleted successfully");
                    return Ok(new { message = "eBay credentials deleted successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to delete eBay credentials" });
                }            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete eBay credentials");
                return StatusCode(500, new { message = "Failed to delete credentials", details = ex.Message });        }
        }
    }

    public class TestEncryptionRequest
    {
        public string TestValue { get; set; } = string.Empty;
    }
}
