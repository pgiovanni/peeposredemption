using Microsoft.Extensions.Configuration;
using Resend;

namespace peeposredemption.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly string _fromAddress;
        private readonly string _adminEmail;

        public EmailService(IResend resend, IConfiguration config)
        {
            _resend = resend;
            _fromAddress = config["Email:From"] ?? "noreply@torvex.app";
            _adminEmail = config["Email:AdminEmail"] ?? "pgiovanni1234@gmail.com";
        }

        public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
        {
            var message = new EmailMessage
            {
                From = $"PeePo's Redemption <{_fromAddress}>",
                To = { toEmail },
                Subject = "Confirm your email",
                HtmlBody = $"<p>Click the link below to confirm your email address:</p><p><a href=\"{confirmationLink}\">Confirm Email</a></p>"
            };

            await _resend.EmailSendAsync(message);
        }

        public async Task SendNewUserNotificationAsync(string username, string email)
        {
            var message = new EmailMessage
            {
                From = $"PeePo's Redemption <{_fromAddress}>",
                To = { _adminEmail },
                Subject = "[Torvex] New user registered",
                HtmlBody = $"<p><strong>{username}</strong> just registered with email <strong>{email}</strong>.</p>"
            };

            await _resend.EmailSendAsync(message);
        }

        public async Task SendMaliciousLinkAlertAsync(string fromUsername, Guid channelId, string content)
        {
            var message = new EmailMessage
            {
                From = $"PeePo's Redemption <{_fromAddress}>",
                To = { _adminEmail },
                Subject = "[ALERT] IP logger link blocked",
                HtmlBody = $"<p><strong>User:</strong> {fromUsername}<br/><strong>Channel:</strong> {channelId}<br/><strong>Content:</strong> {System.Net.WebUtility.HtmlEncode(content)}</p>"
            };

            await _resend.EmailSendAsync(message);
        }
    }
}
