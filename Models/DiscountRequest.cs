namespace API_DigiBook.Models
{
    /// <summary>
    /// Request model for calculating discount
    /// </summary>
    public class DiscountRequest
    {
        public double BasePrice { get; set; }
        public string ItemName { get; set; } = "Item";
        public int Quantity { get; set; } = 1;
        public List<DiscountItem> Discounts { get; set; } = new();
    }

    /// <summary>
    /// Individual discount item
    /// </summary>
    public class DiscountItem
    {
        public string Type { get; set; } = string.Empty;
        public double? Value { get; set; }
        public string? CouponCode { get; set; }
        public string? MembershipTier { get; set; }
        public string? SeasonName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsPercentage { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Response model for discount calculation
    /// </summary>
    public class DiscountResponse
    {
        public double OriginalPrice { get; set; }
        public double FinalPrice { get; set; }
        public double TotalDiscount { get; set; }
        public double DiscountPercentage { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> AppliedDiscounts { get; set; } = new();
    }
}
