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

        public AdminController(IMembershipService membershipService, ILogger<AdminController> logger)
        {
            _membershipService = membershipService;
            _logger = logger;
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
    }
}
