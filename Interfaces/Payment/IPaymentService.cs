using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Payment
{
    public interface IPaymentService
    {
        /// <summary>
        /// Tạo link thanh toán
        /// </summary>
        Task<PaymentResponse> CreatePaymentLinkAsync(PaymentRequest request);

        /// <summary>
        /// Xác thực thanh toán
        /// </summary>
        Task<PaymentVerification> VerifyPaymentAsync(string orderId);

        /// <summary>
        /// Xử lý callback từ payment gateway
        /// </summary>
        Task<bool> HandleCallbackAsync(Dictionary<string, string> callbackData);

        /// <summary>
        /// Lấy tên provider
        /// </summary>
        string GetProviderName();
    }
}
