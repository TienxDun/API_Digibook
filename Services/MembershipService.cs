using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Models;
using Google.Cloud.Firestore;

namespace API_DigiBook.Services
{
    public class MembershipService : IMembershipService
    {
        private const double MemberThreshold = 1_000_000;
        private const double VipThreshold = 5_000_000;

        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ICacheService _cache;
        private readonly ILogger<MembershipService> _logger;

        public MembershipService(
            IUserRepository userRepository,
            IOrderRepository orderRepository,
            ICacheService cache,
            ILogger<MembershipService> logger)
        {
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<User?> RefreshMembershipAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var orders = await _orderRepository.GetByUserIdAsync(userId);
            var totalSpent = orders
                .Where(IsEligibleOrder)
                .Sum(order => order.Payment?.Total ?? 0);

            var nextTier = ResolveTier(totalSpent, user.MembershipTier);
            var nextExpiry = nextTier == "wholesale" ? user.MembershipExpiry ?? string.Empty : string.Empty;

            var totalSpentChanged = Math.Abs(user.TotalSpent - totalSpent) > 0.5d;
            var tierChanged = !string.Equals(user.MembershipTier, nextTier, StringComparison.OrdinalIgnoreCase);
            var expiryChanged = !string.Equals(user.MembershipExpiry, nextExpiry, StringComparison.Ordinal);

            if (!totalSpentChanged && !tierChanged && !expiryChanged)
            {
                return user;
            }

            await _userRepository.UpdateFieldsAsync(userId, new Dictionary<string, object?>
            {
                ["totalSpent"] = totalSpent,
                ["membershipTier"] = nextTier,
                ["membershipExpiry"] = nextExpiry,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            _cache.BumpVersion("users");

            if (tierChanged)
            {
                _logger.LogInformation(
                    "Membership upgraded for user {UserId}: {PreviousTier} -> {NextTier} at totalSpent={TotalSpent}",
                    userId,
                    user.MembershipTier,
                    nextTier,
                    totalSpent);
            }

            return await _userRepository.GetByIdAsync(userId);
        }

        public async Task<int> SyncAllUsersMembershipAsync()
        {
            var users = await _userRepository.GetAllAsync();
            int updatedCount = 0;

            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.Id)) continue;

                try
                {
                    await RefreshMembershipAsync(user.Id);
                    updatedCount++;
                    
                    // Small delay to avoid hammering the DB/Firestore too hard in a tight loop
                    if (updatedCount % 20 == 0)
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing membership for user {UserId}", user.Id);
                }
            }

            _logger.LogInformation("Batch membership sync completed. Updated {Count} users.", updatedCount);
            return updatedCount;
        }

        private static string ResolveTier(double totalSpent, string? currentTier)
        {
            if (string.Equals(currentTier, "wholesale", StringComparison.OrdinalIgnoreCase))
            {
                return "wholesale";
            }

            if (totalSpent >= VipThreshold)
            {
                return "vip";
            }

            if (totalSpent >= MemberThreshold)
            {
                return "member";
            }

            return "regular";
        }

        private static bool IsEligibleOrder(Order order)
        {
            var status = order.Status?.Trim().ToLowerInvariant();
            var paymentStatus = order.Payment?.Status?.Trim().ToUpperInvariant();

            var isCancelled = order.StatusStep == 4 || status == "đã hủy";
            var isFailedPayment = paymentStatus == "FAILED" || paymentStatus == "CANCELLED";
            var isPaid = paymentStatus == "PAID";
            var isDelivered = order.StatusStep == 3 || status == "đã giao";

            return !isCancelled && !isFailedPayment && (isPaid || isDelivered);
        }
    }
}
