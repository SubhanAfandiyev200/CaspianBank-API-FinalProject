using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Service.Helpers.Settings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class MailKitEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;

        public MailKitEmailSender(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var message = new MimeMessage();
            var fromAddress = string.IsNullOrWhiteSpace(_settings.FromAddress) ? _settings.UserName : _settings.FromAddress;
            message.From.Add(new MailboxAddress(_settings.FromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new BodyBuilder
            {
                HtmlBody = htmlBody
            }.ToMessageBody();

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = _settings.CheckCertificateRevocation;
            // 587 portunda bağlantı STARTTLS ilə şifrələnir (Gmail üçün)
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
