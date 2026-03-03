namespace API_DigiBook.Models
{
    public class PaymentVerification
    {
        public bool IsValid { get; set; }
        public string Status { get; set; } = string.Empty; // "PENDING", "PAID", "CANCELLED"
        public string OrderId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public double Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
