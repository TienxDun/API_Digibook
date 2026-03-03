using API_DigiBook.Interfaces.Payment;
using API_DigiBook.Services.Payment;

namespace API_DigiBook.Factories
{
    public enum PaymentMethod
    {
        COD,
        PayOS
    }

    public class PaymentServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PaymentServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPaymentService? CreatePaymentService(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.PayOS => _serviceProvider.GetService<PayOSService>(),
                PaymentMethod.COD => null, // COD không cần payment service
                _ => throw new NotSupportedException($"Payment method {method} is not supported")
            };
        }

        public IPaymentService? CreatePaymentService(string methodName)
        {
            var method = methodName.ToUpper() switch
            {
                "PAYOS" => PaymentMethod.PayOS,
                "COD" => PaymentMethod.COD,
                _ => throw new NotSupportedException($"Payment method {methodName} is not supported")
            };

            return CreatePaymentService(method);
        }
    }
}
