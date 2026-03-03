namespace API_DigiBook.Models
{
    public class PaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public CustomerInfo Customer { get; set; } = new CustomerInfo();
        public List<PaymentItem> Items { get; set; } = new List<PaymentItem>();
    }

    public class CustomerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class PaymentItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
    }
}
