using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class PaymentTransaction
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [FirestoreProperty("orderCode")]
        public string OrderCode { get; set; } = string.Empty;

        [FirestoreProperty("provider")]
        public string Provider { get; set; } = string.Empty; // "COD", "PayOS"

        [FirestoreProperty("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [FirestoreProperty("paymentLinkId")]
        public string PaymentLinkId { get; set; } = string.Empty;

        [FirestoreProperty("amount")]
        public double Amount { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = "PENDING"; // "PENDING", "PAID", "CANCELLED", "FAILED"

        [FirestoreProperty("checkoutUrl")]
        public string CheckoutUrl { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }

        [FirestoreProperty("paidAt")]
        public Timestamp? PaidAt { get; set; }

        [FirestoreProperty("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
