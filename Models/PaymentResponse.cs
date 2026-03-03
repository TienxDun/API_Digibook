namespace API_DigiBook.Models
{
    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
    }
}
