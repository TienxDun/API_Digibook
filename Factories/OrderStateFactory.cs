using API_DigiBook.Interfaces.States;
using API_DigiBook.States.Orders;

namespace API_DigiBook.Factories
{
    /// <summary>
    /// Factory to get the appropriate Order State based on the status string
    /// </summary>
    public static class OrderStateFactory
    {
        public static IOrderState GetState(string status)
        {
            return status switch
            {
                "Đang xử lý" => new PendingState(),
                "Đã xác nhận" => new ConfirmedState(),
                "Đang đóng gói" => new PackingState(),
                "Đang giao" => new ShippingState(),
                "Đã giao" => new DeliveredState(),
                "Đã hủy" => new CanceledState(),
                "Giao thất bại" => new FailedDeliveryState(),
                _ => new PendingState() // Default to initial state
            };
        }
    }
}
