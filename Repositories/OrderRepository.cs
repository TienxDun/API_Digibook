using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;
using Google.Cloud.Firestore;
using System.Linq;

namespace API_DigiBook.Repositories
{
    public class OrderRepository : FirestoreRepository<Order>, IOrderRepository
    {
        public OrderRepository(ICacheService cache, ILogger<OrderRepository> logger) 
            : base("orders", cache, logger)
        {
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
        {
            try
            {
                // Query without ordering first, as some old orders may not have createdAt
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("userId", userId);
                
                var snapshot = await query.GetSnapshotAsync();
                var orders = new List<Order>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var order = document.ConvertTo<Order>();
                        order.Id = document.Id;
                        orders.Add(order);
                    }
                }

                // Sort in memory by createdAt if available, otherwise by date string
                return orders.OrderByDescending(o => 
                    !o.CreatedAt.Equals(default(Timestamp)) 
                        ? o.CreatedAt.ToDateTime() 
                        : DateTime.TryParse(o.Date, out var dt) ? dt : DateTime.MinValue
                ).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting orders by user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetByStatusAsync(string status)
        {
            try
            {
                // Case-insensitive status search
                var allOrders = await GetAllAsync();
                return allOrders
                    .Where(o => string.Equals(o.Status, status, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(o => o.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting orders by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .OrderByDescending("createdAt")
                    .Limit(count);
                
                var snapshot = await query.GetSnapshotAsync();
                var orders = new List<Order>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var order = document.ConvertTo<Order>();
                        order.Id = document.Id;
                        orders.Add(order);
                    }
                }

                return orders;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recent orders");
                throw;
            }
        }

        public async Task<double> GetTotalRevenueAsync()
        {
            try
            {
                // Note: Better would be a dedicated stats collection updated via Cloud Functions
                // or using Firestore's built-in sum (requires newer SDKs/logic).
                // Here we fetch only what's needed to reduce payload cost.
                var query = _db.Collection(_collectionName).Select("payment.total");
                var snapshot = await query.GetSnapshotAsync();
                
                double total = 0;
                foreach (var doc in snapshot.Documents)
                {
                    if (doc.ContainsField("payment.total"))
                    {
                        total += doc.GetValue<double>("payment.total");
                    }
                }
                return total;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating total revenue");
                return 0;
            }
        }

        public async Task<(int pending, int completed, int today)> GetOrderStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                var todayStartTimestamp = Timestamp.FromDateTime(todayStart);

                var pendingTask = _db.Collection(_collectionName).WhereLessThan("statusStep", 4).Count().GetSnapshotAsync();
                var completedTask = _db.Collection(_collectionName).WhereEqualTo("statusStep", 4).Count().GetSnapshotAsync();
                var todayTask = _db.Collection(_collectionName).WhereGreaterThanOrEqualTo("createdAt", todayStartTimestamp).Count().GetSnapshotAsync();

                await Task.WhenAll(pendingTask, completedTask, todayTask);

                return ((int)(pendingTask.Result.Count ?? 0L), (int)(completedTask.Result.Count ?? 0L), (int)(todayTask.Result.Count ?? 0L));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting order stats");
                return (0, 0, 0);
            }
        }

        public async Task<IEnumerable<dynamic>> GetRevenueByDayAsync(int days = 7)
        {
            try
            {
                var now = DateTime.UtcNow;
                var startDate = now.Date.AddDays(-days + 1);
                var startDateTimestamp = Timestamp.FromDateTime(startDate.ToUniversalTime());

                var query = _db.Collection(_collectionName)
                    .WhereGreaterThanOrEqualTo("createdAt", startDateTimestamp)
                    .OrderBy("createdAt");
                
                var snapshot = await query.GetSnapshotAsync();
                
                // Group by day in memory
                var dailyRevenue = snapshot.Documents
                    .Select(doc => new { 
                        Date = doc.GetValue<Timestamp>("createdAt").ToDateTime().Date, 
                        Total = doc.ContainsField("payment.total") ? doc.GetValue<double>("payment.total") : 0 
                    })
                    .GroupBy(x => x.Date)
                    .Select(g => new { 
                        day = g.Key.Day.ToString(), 
                        month = g.Key.Month.ToString(), 
                        total = g.Sum(x => x.Total) 
                    })
                    .ToList();

                return dailyRevenue;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting revenue by day");
                return Enumerable.Empty<dynamic>();
            }
        }
    }
}
