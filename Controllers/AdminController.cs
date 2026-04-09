using API_DigiBook.Interfaces.Services;
using API_DigiBook.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly ILogger<AdminController> _logger;
        private readonly ITikiService _tikiService;
        private readonly IBookRepository _bookRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly ICouponRepository _couponRepository;

        public AdminController(
            IMembershipService membershipService,
            ILogger<AdminController> logger,
            ITikiService tikiService,
            IBookRepository bookRepository,
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IAuthorRepository authorRepository,
            ICouponRepository couponRepository)
        {
            _membershipService = membershipService;
            _logger = logger;
            _tikiService = tikiService;
            _bookRepository = bookRepository;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _authorRepository = authorRepository;
            _couponRepository = couponRepository;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var booksCountTask = _bookRepository.CountAsync();
                var ordersCountTask = _orderRepository.CountAsync();
                var usersCountTask = _userRepository.CountAsync();
                var categoriesCountTask = _categoryRepository.CountAsync();
                var authorsCountTask = _authorRepository.CountAsync();
                var couponsCountTask = _couponRepository.CountAsync();
                var revenueTask = _orderRepository.GetTotalRevenueAsync();
                
                var stockStatsTask = _bookRepository.GetStockStatsAsync();
                var orderStatsTask = _orderRepository.GetOrderStatsAsync();
                var recentOrdersTask = _orderRepository.GetRecentOrdersAsync(5);
                var revenueByDayTask = _orderRepository.GetRevenueByDayAsync(7);
                var topSellingTask = _bookRepository.GetTopSellingBooksAsync(5);

                await Task.WhenAll(
                    booksCountTask, ordersCountTask, usersCountTask, 
                    categoriesCountTask, authorsCountTask, couponsCountTask, 
                    revenueTask, stockStatsTask, orderStatsTask, 
                    recentOrdersTask, revenueByDayTask, topSellingTask
                );
                
                var (lowStock, outOfStock) = await stockStatsTask;
                var (pendingOrders, completedOrders, todayOrders) = await orderStatsTask;
                var totalRevenue = await revenueTask;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalBooks = await booksCountTask,
                        totalOrders = await ordersCountTask,
                        totalUsers = await usersCountTask,
                        totalCategories = await categoriesCountTask,
                        totalAuthors = await authorsCountTask,
                        totalCoupons = await couponsCountTask,
                        totalRevenue = totalRevenue,
                        lowStock = lowStock,
                        outOfStock = outOfStock,
                        pendingOrders = pendingOrders,
                        completedOrders = completedOrders,
                        todayOrders = todayOrders,
                        avgOrderValue = (await ordersCountTask) > 0 ? totalRevenue / (await ordersCountTask) : 0,
                        recentOrders = await recentOrdersTask,
                        revenueByDay = await revenueByDayTask,
                        topSellingBooks = await topSellingTask,
                        maxRevenue = (await revenueByDayTask).Any() ? (await revenueByDayTask).Max(r => (double)r.total) : 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching admin summary");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
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
                var content = await _tikiService.GetTikiDataAsStringAsync(url);
                return Content(content, "application/json");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode((int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), new
                {
                    success = false,
                    message = "Failed to fetch Tiki data.",
                    statusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError)
                });
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
