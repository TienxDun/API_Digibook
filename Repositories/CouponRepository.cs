using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;
using Google.Cloud.Firestore;

namespace API_DigiBook.Repositories
{
    public class CouponRepository : FirestoreRepository<Coupon>, ICouponRepository
    {
        public CouponRepository(ICacheService cache, ILogger<CouponRepository> logger) 
            : base("coupons", cache, logger)
        {
        }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            try
            {
                // Case-insensitive coupon code search
                var allCoupons = await GetAllAsync();
                return allCoupons.FirstOrDefault(c => 
                    string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting coupon by code {Code}", code);
                throw;
            }
        }

        public async Task<IEnumerable<Coupon>> GetActiveAsync()
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("isActive", true);
                
                var snapshot = await query.GetSnapshotAsync();
                var coupons = new List<Coupon>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var coupon = document.ConvertTo<Coupon>();
                        coupon.Id = document.Id;
                        
                        // Check expiry date
                        if (!string.IsNullOrEmpty(coupon.ExpiryDate))
                        {
                            if (DateTime.TryParse(coupon.ExpiryDate, out var expiryDate))
                            {
                                if (expiryDate >= DateTime.Now)
                                {
                                    coupons.Add(coupon);
                                }
                            }
                        }
                        else
                        {
                            coupons.Add(coupon);
                        }
                    }
                }

                return coupons;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting active coupons");
                throw;
            }
        }

        public async Task<bool> IncrementUsageAsync(string id)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                await docRef.UpdateAsync("usedCount", FieldValue.Increment(1));
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error incrementing usage for coupon {Id}", id);
                throw;
            }
        }
    }
}
