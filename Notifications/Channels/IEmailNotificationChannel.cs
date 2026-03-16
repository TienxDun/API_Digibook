using API_DigiBook.Notifications.Models;

namespace API_DigiBook.Notifications.Channels
{
    public interface IEmailNotificationChannel
    {
        Task<NotificationChannelResult> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default);
    }
}
