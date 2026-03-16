using System.Net.Http.Json;
using System.Text.Json.Serialization;
using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Models;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Notifications.Channels
{
    public class ResendEmailNotificationChannel : IEmailNotificationChannel
    {
        private readonly HttpClient _httpClient;
        private readonly NotificationOptions _options;
        private readonly ILogger<ResendEmailNotificationChannel> _logger;

        public ResendEmailNotificationChannel(
            HttpClient httpClient,
            IOptions<NotificationOptions> options,
            ILogger<ResendEmailNotificationChannel> logger)
        {
            _httpClient = httpClient;
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
            var apiKey = _options.Email.Resend.ApiKey?.Trim() ?? string.Empty;
            var fromEmail = _options.Email.FromEmail?.Trim() ?? string.Empty;
            var fromName = _options.Email.FromName?.Trim() ?? "DigiBook";

            if (string.IsNullOrWhiteSpace(recipient))
            {
                return NotificationChannelResult.Fail("Recipient email is empty.");
            }

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
            {
                return NotificationChannelResult.Fail("Resend configuration is incomplete.");
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/emails");
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

                var payload = new ResendEmailRequest
                {
                    From = string.IsNullOrWhiteSpace(fromName)
                        ? fromEmail
                        : $"{fromName} <{fromEmail}>",
                    To = new[] { recipient },
                    Subject = subject,
                    Html = htmlBody
                };

                request.Content = JsonContent.Create(payload);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return NotificationChannelResult.Fail($"Resend API error: {(int)response.StatusCode} - {body}");
                }

                return NotificationChannelResult.Ok($"Resend accepted: {body}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resend send failed. To={To}, From={From}", recipient, fromEmail);
                return NotificationChannelResult.Fail(ex.Message);
            }
        }

        public static bool IsConfigured(NotificationOptions options)
        {
            return !string.IsNullOrWhiteSpace(options.Email.Resend.ApiKey) &&
                   !string.IsNullOrWhiteSpace(options.Email.FromEmail);
        }

        private sealed class ResendEmailRequest
        {
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public string[] To { get; set; } = Array.Empty<string>();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("html")]
            public string Html { get; set; } = string.Empty;
        }
    }
}
