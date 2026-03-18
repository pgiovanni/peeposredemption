using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace peeposredemption.Application.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;

        public SmtpEmailService(IConfiguration config)
        {
            _host = config["Smtp:Host"] ?? "localhost";
            _port = int.Parse(config["Smtp:Port"] ?? "1025");
        }

        public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
        {
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = false,
                Credentials = CredentialCache.DefaultNetworkCredentials
            };

            var message = new MailMessage
            {
                From = new MailAddress("noreply@peeposredemption.local", "PeePo's Redemption"),
                Subject = "Confirm your email",
                Body = $"<p>Click the link below to confirm your email address:</p><p><a href=\"{confirmationLink}\">Confirm Email</a></p>",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }

        public Task SendNewUserNotificationAsync(string username, string email) => Task.CompletedTask;
        public Task SendReferralPurchaseNotificationAsync(string marketerUsername, string buyerUsername, long amountCents) => Task.CompletedTask;
        public Task SendArtistSubmissionNotificationAsync(string displayName, string email, string portfolioUrl) => Task.CompletedTask;

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = false,
                Credentials = CredentialCache.DefaultNetworkCredentials
            };

            var message = new MailMessage
            {
                From = new MailAddress("noreply@peeposredemption.local", "PeePo's Redemption"),
                Subject = "Reset your password",
                Body = $"<p>Click the link below to reset your password. This link expires in 30 minutes.</p><p><a href=\"{resetLink}\">Reset Password</a></p>",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }

        public async Task SendMaliciousLinkAlertAsync(string fromUsername, Guid channelId, string content)
        {
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = false,
                Credentials = CredentialCache.DefaultNetworkCredentials
            };

            var message = new MailMessage
            {
                From = new MailAddress("noreply@peeposredemption.local", "PeePo's Redemption"),
                Subject = "[ALERT] IP logger link blocked",
                Body = $"<p><strong>User:</strong> {fromUsername}<br/><strong>Channel:</strong> {channelId}<br/><strong>Content:</strong> {System.Net.WebUtility.HtmlEncode(content)}</p>",
                IsBodyHtml = true
            };
            message.To.Add("pgiovanni1234@gmail.com");

            await client.SendMailAsync(message);
        }
    }
}
