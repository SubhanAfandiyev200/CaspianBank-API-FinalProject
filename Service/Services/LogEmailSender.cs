using Microsoft.Extensions.Logging;
using Service.Services.Interfaces;

namespace Service.Services
{
    // SMTP qurulmayıbsa: email göndərilmir, mətn konsola yazılır (OTP kodunu sınamaq üçün).
    public class LogEmailSender : IEmailSender
    {
        private readonly ILogger<LogEmailSender> _logger;

        public LogEmailSender(ILogger<LogEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("SMTP qurulmayıb, email göndərilmədi. To: {To} | Subject: {Subject} | Body: {Body}", to, subject, htmlBody);
            return Task.CompletedTask;
        }
    }
}
