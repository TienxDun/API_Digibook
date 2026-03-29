using API_DigiBook.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly ILogger<AdminController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(
            IMembershipService membershipService,
            ILogger<AdminController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _membershipService = membershipService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Sync all users membership data (TotalSpent and Tier)
        /// Use this after launching the Ranking feature to catch up with old order history.
        /// </summary>
        [HttpPost("membership/sync-all")]
        public async Task<IActionResult> SyncAllMembership()
        {
            try
            {
                _logger.LogInformation("Admin requested full membership sync.");
                int count = await _membershipService.SyncAllUsersMembershipAsync();
                
                return Ok(new
                {
                    success = true,
                    message = $"Successfully synced membership for {count} users.",
                    updatedCount = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during full membership sync");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during synchronization.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("tiki-proxy")]
        public async Task<IActionResult> GetTikiProxy([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Url is required."
                    });
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid target url."
                    });
                }

                if (!string.Equals(targetUri.Host, "tiki.vn", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Only tiki.vn is allowed."
                    });
                }

                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, targetUri);
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 DigiBook/1.0");
                request.Headers.TryAddWithoutValidation("Accept", "application/json,text/plain,*/*");
                request.Headers.TryAddWithoutValidation("Referer", "https://tiki.vn/");

                using var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Tiki proxy failed with status {StatusCode} for {TargetUrl}",
                        (int)response.StatusCode,
                        targetUri);

                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        message = "Failed to fetch Tiki data.",
                        statusCode = (int)response.StatusCode
                    });
                }

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proxying Tiki request: {TargetUrl}", url);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while fetching Tiki data.",
                    error = ex.Message
                });
            }
        }
    }
}
