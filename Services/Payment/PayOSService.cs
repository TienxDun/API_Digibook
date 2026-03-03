using API_DigiBook.Interfaces.Payment;
using API_DigiBook.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace API_DigiBook.Services.Payment
{
    public class PayOSService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _apiBaseUrl = "https://api-merchant.payos.vn/v2/payment-requests";

        public PayOSService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _clientId = _configuration["PayOS:ClientId"] ?? throw new ArgumentNullException("PayOS:ClientId");
            _apiKey = _configuration["PayOS:ApiKey"] ?? throw new ArgumentNullException("PayOS:ApiKey");
            _checksumKey = _configuration["PayOS:ChecksumKey"] ?? throw new ArgumentNullException("PayOS:ChecksumKey");

            // Setup HttpClient headers
            _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        }

        public string GetProviderName() => "PayOS";

        public async Task<PaymentResponse> CreatePaymentLinkAsync(PaymentRequest request)
        {
            try
            {
                var orderCode = long.Parse(request.OrderCode);
                
                // Chuẩn bị payload theo format PayOS
                var paymentData = new
                {
                    orderCode = orderCode,
                    amount = (int)request.Amount,
                    description = request.Description,
                    returnUrl = request.ReturnUrl,
                    cancelUrl = request.CancelUrl,
                    items = request.Items.Select(item => new
                    {
                        name = item.Name,
                        quantity = item.Quantity,
                        price = (int)item.Price
                    }).ToList(),
                    buyerName = request.Customer.Name,
                    buyerEmail = request.Customer.Email,
                    buyerPhone = request.Customer.Phone
                };

                // Tạo signature
                var signature = CreateSignature(
                    paymentData.amount,
                    paymentData.cancelUrl,
                    paymentData.description,
                    paymentData.orderCode,
                    paymentData.returnUrl
                );
                
                // Thêm signature vào request
                var requestData = new
                {
                    orderCode = paymentData.orderCode,
                    amount = paymentData.amount,
                    description = paymentData.description,
                    returnUrl = paymentData.returnUrl,
                    cancelUrl = paymentData.cancelUrl,
                    items = paymentData.items,
                    buyerName = paymentData.buyerName,
                    buyerEmail = paymentData.buyerEmail,
                    buyerPhone = paymentData.buyerPhone,
                    signature = signature
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"[PayOS] Request URL: {_apiBaseUrl}");
                Console.WriteLine($"[PayOS] Request Body: {json}");
                Console.WriteLine($"[PayOS] ClientId: {_clientId}");

                var response = await _httpClient.PostAsync(_apiBaseUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[PayOS] Response Status: {response.StatusCode}");
                Console.WriteLine($"[PayOS] Response Body: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PayOSCreateResponse>(responseContent);
                    
                    Console.WriteLine($"[PayOS] CheckoutUrl: {result?.data?.checkoutUrl}");
                    Console.WriteLine($"[PayOS] PaymentLinkId: {result?.data?.paymentLinkId}");
                    
                    return new PaymentResponse
                    {
                        Success = true,
                        Message = "Payment link created successfully",
                        CheckoutUrl = result?.data?.checkoutUrl ?? string.Empty,
                        PaymentLinkId = result?.data?.paymentLinkId ?? string.Empty,
                        OrderCode = request.OrderCode,
                        QrCode = result?.data?.qrCode ?? string.Empty
                    };
                }
                else
                {
                    Console.WriteLine($"[PayOS] Error Response: {responseContent}");
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = $"Failed to create payment link: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Error creating payment link: {ex.Message}"
                };
            }
        }

        public async Task<PaymentVerification> VerifyPaymentAsync(string paymentLinkId)
        {
            try
            {
                // PayOS API: GET /v2/payment-requests/{paymentLinkId}
                var url = $"{_apiBaseUrl}/{paymentLinkId}";
                
                Console.WriteLine($"[PayOS] Verifying payment: {url}");
                
                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[PayOS] Verify Response Status: {response.StatusCode}");
                Console.WriteLine($"[PayOS] Verify Response Body: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PayOSQueryResponse>(responseContent);
                    
                    var paymentStatus = result?.data?.status ?? "PENDING";
                    
                    Console.WriteLine($"[PayOS] Payment Status: {paymentStatus}");
                    
                    return new PaymentVerification
                    {
                        IsValid = true,
                        Status = paymentStatus,
                        OrderId = result?.data?.orderCode.ToString() ?? string.Empty,
                        TransactionId = result?.data?.transactionId ?? string.Empty,
                        Amount = result?.data?.amount ?? 0,
                        PaidAt = result?.data?.paidAt,
                        Message = "Verification successful"
                    };
                }
                else
                {
                    return new PaymentVerification
                    {
                        IsValid = false,
                        Message = $"Failed to verify payment: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS] Verify Error: {ex.Message}");
                return new PaymentVerification
                {
                    IsValid = false,
                    Message = $"Error verifying payment: {ex.Message}"
                };
            }
        }

        public async Task<bool> HandleCallbackAsync(Dictionary<string, string> callbackData)
        {
            try
            {
                // Verify webhook signature
                if (!callbackData.ContainsKey("signature"))
                    return false;

                var receivedSignature = callbackData["signature"];
                var dataToSign = string.Join("&", callbackData
                    .Where(kvp => kvp.Key != "signature")
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => $"{kvp.Key}={kvp.Value}"));

                var expectedSignature = ComputeHmacSha256(dataToSign, _checksumKey);

                if (receivedSignature != expectedSignature)
                    return false;

                // Signature is valid
                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        private string CreateSignature(int amount, string cancelUrl, string description, long orderCode, string returnUrl)
        {
            // PayOS yêu cầu signature từ: amount, cancelUrl, description, orderCode, returnUrl
            // Được sắp xếp theo alphabet và format: key1=value1&key2=value2
            
            var signatureData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            
            Console.WriteLine($"[PayOS] Signature Data: {signatureData}");
            
            return ComputeHmacSha256(signatureData, _checksumKey);
        }

        private string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        // Response DTOs
        private class PayOSCreateResponse
        {
            public string code { get; set; } = string.Empty;
            public string desc { get; set; } = string.Empty;
            public PayOSData? data { get; set; }
        }

        private class PayOSData
        {
            public string paymentLinkId { get; set; } = string.Empty;
            public long orderCode { get; set; }
            public string checkoutUrl { get; set; } = string.Empty;
            public string qrCode { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public double amount { get; set; }
            public string transactionId { get; set; } = string.Empty;
            public DateTime? paidAt { get; set; }
        }

        private class PayOSQueryResponse
        {
            public string code { get; set; } = string.Empty;
            public string desc { get; set; } = string.Empty;
            public PayOSData? data { get; set; }
        }
    }
}
