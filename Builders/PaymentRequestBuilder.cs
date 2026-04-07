using API_DigiBook.Models;

namespace API_DigiBook.Builders
{
    /// <summary>
    /// Builder Pattern: Encapsulates the complex construction logic of PaymentRequest.
    /// Provides a fluent interface for building PaymentRequest objects step-by-step.
    /// </summary>
    public class PaymentRequestBuilder
    {
        private readonly PaymentRequest _request = new PaymentRequest();

        /// <summary>
        /// Sets the core order details: a unique order code, total amount, and description.
        /// </summary>
        public PaymentRequestBuilder WithOrderDetails(string orderId, double amount, string description)
        {
            _request.OrderId = orderId;
            _request.OrderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            _request.Amount = amount;
            _request.Description = description;
            return this;
        }

        /// <summary>
        /// Sets the return and cancel URLs for after the payment is completed or cancelled.
        /// </summary>
        public PaymentRequestBuilder WithUrls(string baseUrl, string orderId)
        {
            _request.ReturnUrl = $"{baseUrl}/order-success?orderId={orderId}";
            _request.CancelUrl = $"{baseUrl}/payment-cancel?orderId={orderId}";
            return this;
        }

        /// <summary>
        /// Sets customer information from the order's customer data.
        /// </summary>
        public PaymentRequestBuilder WithCustomer(CustomerInfo customer)
        {
            _request.Customer = new CustomerInfo
            {
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone
            };
            return this;
        }

        /// <summary>
        /// Converts the order items into PaymentItems and attaches them to the request.
        /// </summary>
        public PaymentRequestBuilder WithItems(IEnumerable<PaymentItem> items)
        {
            _request.Items = items.ToList();
            return this;
        }

        /// <summary>
        /// Validates all required fields and returns the fully constructed PaymentRequest.
        /// Throws InvalidOperationException if required fields are missing.
        /// </summary>
        public PaymentRequest Build()
        {
            if (string.IsNullOrWhiteSpace(_request.OrderId))
                throw new InvalidOperationException("PaymentRequest requires OrderId. Call WithOrderDetails() first.");

            if (_request.Amount <= 0)
                throw new InvalidOperationException("PaymentRequest requires a positive Amount. Call WithOrderDetails() first.");

            if (string.IsNullOrWhiteSpace(_request.ReturnUrl))
                throw new InvalidOperationException("PaymentRequest requires ReturnUrl. Call WithUrls() first.");

            return _request;
        }
    }
}
