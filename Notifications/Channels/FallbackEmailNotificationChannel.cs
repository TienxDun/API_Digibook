using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Models;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Notifications.Channels
{
    public class FallbackEmailNotificationChannel : IEmailNotificationChannel
    {
        private readonly ResendEmailNotificationChannel _resendChannel;
        private readonly SmtpEmailNotificationChannel _smtpChannel;
        private readonly NotificationOptions _options;
        private readonly ILogger<FallbackEmailNotificationChannel> _logger;

        public FallbackEmailNotificationChannel(
            ResendEmailNotificationChannel resendChannel,
            SmtpEmailNotificationChannel smtpChannel,
            IOptions<NotificationOptions> options,
            ILogger<FallbackEmailNotificationChannel> logger)
        {
            _resendChannel = resendChannel;
            _smtpChannel = smtpChannel;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<NotificationChannelResult> SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            var preferredProvider = (_options.Email.Provider ?? string.Empty).Trim().ToLowerInvariant();
            var resendConfigured = ResendEmailNotificationChannel.IsConfigured(_options);

            var primary = preferredProvider == "smtp" ? "smtp" : "resend";
            if (primary == "resend" && !resendConfigured)
            {
                primary = "smtp";
            }

            if (primary == "resend")
            {
                var resendResult = await _resendChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (resendResult.Success)
                {
                    return resendResult;
                }

                _logger.LogWarning("Resend failed, fallback to SMTP. Error={Error}", resendResult.ErrorMessage);
                var smtpResult = await _smtpChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
                if (smtpResult.Success)
                {
                    return NotificationChannelResult.Ok($"Fallback SMTP accepted after Resend failure: {resendResult.ErrorMessage}");
                }

                return NotificationChannelResult.Fail(
                    $"Resend failed: {resendResult.ErrorMessage} | SMTP fallback failed: {smtpResult.ErrorMessage}");
            }

            var primarySmtp = await _smtpChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
            if (primarySmtp.Success)
            {
                return primarySmtp;
            }

            if (!resendConfigured)
            {
                return primarySmtp;
            }

            _logger.LogWarning("SMTP failed, fallback to Resend. Error={Error}", primarySmtp.ErrorMessage);
            var resendFallback = await _resendChannel.SendAsync(toEmail, subject, htmlBody, cancellationToken);
            if (resendFallback.Success)
            {
                return NotificationChannelResult.Ok($"Fallback Resend accepted after SMTP failure: {primarySmtp.ErrorMessage}");
            }

            return NotificationChannelResult.Fail(
                $"SMTP failed: {primarySmtp.ErrorMessage} | Resend fallback failed: {resendFallback.ErrorMessage}");
        }
    }
}
