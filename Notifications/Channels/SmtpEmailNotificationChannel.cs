using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace API_DigiBook.Notifications.Channels
{
    public class SmtpEmailNotificationChannel
    {
        private readonly NotificationOptions _options;
        private readonly ILogger<SmtpEmailNotificationChannel> _logger;

        public SmtpEmailNotificationChannel(
            IOptions<NotificationOptions> options,
            ILogger<SmtpEmailNotificationChannel> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<NotificationChannelResult> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            var recipient = toEmail?.Trim() ?? string.Empty;
            var username = _options.Email.Username?.Trim() ?? string.Empty;
            // Gmail app password is frequently copied with spaces (xxxx xxxx xxxx xxxx).
            var appPassword = new string((_options.Email.AppPassword ?? string.Empty)
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());

            if (string.IsNullOrWhiteSpace(recipient))
            {
                return NotificationChannelResult.Fail("Recipient email is empty.");
            }

            if (string.IsNullOrWhiteSpace(_options.Email.Host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(appPassword) ||
                string.IsNullOrWhiteSpace(_options.Email.FromEmail))
            {
                return NotificationChannelResult.Fail("Email SMTP configuration is incomplete.");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.Email.FromName, _options.Email.FromEmail));
                message.To.Add(MailboxAddress.Parse(recipient));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var smtp = new SmtpClient();
                smtp.Timeout = _options.Email.TimeoutMilliseconds;

                var secureSocketOptions = _options.Email.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

                await smtp.ConnectAsync(
                    _options.Email.Host,
                    _options.Email.Port,
                    secureSocketOptions,
                    cancellationToken);

                await smtp.AuthenticateAsync(
                    username,
                    appPassword,
                    cancellationToken);

                await smtp.SendAsync(message, cancellationToken);
                await smtp.DisconnectAsync(true, cancellationToken);

                return NotificationChannelResult.Ok("SMTP accepted");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SMTP send failed. Host={Host}, Port={Port}, EnableSsl={EnableSsl}, From={FromEmail}",
                    _options.Email.Host,
                    _options.Email.Port,
                    _options.Email.EnableSsl,
                    _options.Email.FromEmail);

                return NotificationChannelResult.Fail(ex.Message);
            }
        }
    }
}
