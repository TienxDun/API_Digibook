using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Notifications.Channels;
using API_DigiBook.Notifications.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/notifications/debug")]
    public class NotificationDebugController : ControllerBase
    {
        private readonly SmtpEmailNotificationChannel _smtpChannel;
        private readonly INotificationLogRepository _notificationLogRepository;
        private readonly NotificationOptions _notificationOptions;
        private readonly ILogger<NotificationDebugController> _logger;

        public NotificationDebugController(
            SmtpEmailNotificationChannel smtpChannel,
            INotificationLogRepository notificationLogRepository,
            IOptions<NotificationOptions> notificationOptions,
            ILogger<NotificationDebugController> logger)
        {
            _smtpChannel = smtpChannel;
            _notificationLogRepository = notificationLogRepository;
            _notificationOptions = notificationOptions.Value;
            _logger = logger;
        }

        [HttpGet("email-config")]
        public IActionResult GetEmailConfig()
        {
            var rawPassword = _notificationOptions.Email.AppPassword ?? string.Empty;
            var normalizedPassword = new string(rawPassword.Where(c => !char.IsWhiteSpace(c)).ToArray());

            return Ok(new
            {
                success = true,
                data = new
                {
                    enableEmail = _notificationOptions.EnableEmail,
                    host = _notificationOptions.Email.Host,
                    port = _notificationOptions.Email.Port,
                    enableSsl = _notificationOptions.Email.EnableSsl,
                    username = _notificationOptions.Email.Username,
                    fromEmail = _notificationOptions.Email.FromEmail,
                    fromName = _notificationOptions.Email.FromName,
                    rawPasswordLength = rawPassword.Length,
                    normalizedPasswordLength = normalizedPassword.Length,
                    hasPassword = !string.IsNullOrWhiteSpace(normalizedPassword)
                }
            });
        }

        [HttpPost("send-test-email")]
        public async Task<IActionResult> SendTestEmail([FromBody] SendTestEmailRequest? request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ToEmail))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "toEmail is required"
                });
            }

            var subject = string.IsNullOrWhiteSpace(request.Subject)
                ? "[DigiBook] SMTP test from Render"
                : request.Subject.Trim();

            var body = string.IsNullOrWhiteSpace(request.HtmlBody)
                ? "<p>This is a test email from DigiBook backend.</p>"
                : request.HtmlBody;

            var result = await _smtpChannel.SendAsync(request.ToEmail.Trim(), subject, body, cancellationToken);

            _logger.LogInformation(
                "Manual SMTP test executed. To={To}, Success={Success}, Error={Error}",
                request.ToEmail,
                result.Success,
                result.ErrorMessage);

            return Ok(new
            {
                success = result.Success,
                providerResponse = result.ProviderResponse,
                errorMessage = result.ErrorMessage
            });
        }

        [HttpGet("email-logs")]
        public async Task<IActionResult> GetLatestEmailLogs([FromQuery] int limit = 20)
        {
            var safeLimit = Math.Clamp(limit, 1, 100);
            var logs = await _notificationLogRepository.GetAllAsync();

            var data = logs
                .Where(log => string.Equals(log.Channel, "email", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(log => log.UpdatedAt.ToDateTime())
                .Take(safeLimit)
                .Select(log => new
                {
                    log.Id,
                    log.EventType,
                    log.Recipient,
                    log.Status,
                    log.Attempt,
                    log.ErrorMessage,
                    log.ProviderResponse,
                    updatedAtUtc = log.UpdatedAt.ToDateTime().ToUniversalTime(),
                    sentAtUtc = log.SentAt?.ToDateTime().ToUniversalTime()
                })
                .ToList();

            return Ok(new
            {
                success = true,
                count = data.Count,
                data
            });
        }

        public class SendTestEmailRequest
        {
            public string ToEmail { get; set; } = string.Empty;
            public string? Subject { get; set; }
            public string? HtmlBody { get; set; }
        }
    }
}
