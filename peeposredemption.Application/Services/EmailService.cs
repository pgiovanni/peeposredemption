using Microsoft.Extensions.Configuration;
using Resend;

namespace peeposredemption.Application.Services
{
    public class EmailService
    {
        private readonly IResend _resend;

        public EmailService(IResend resend)
        {
            _resend = resend;
        }

        public async Task SendConfirmationEmailAsync(string toEmail, string confirmationLink)
        {
            var message = new EmailMessage
            {
                From = "PeePo's Redemption <onboarding@resend.dev>",
                To = { toEmail },
                Subject = "Confirm your email",
                HtmlBody = $"<p>Click the link below to confirm your email address:</p><p><a href=\"{confirmationLink}\">Confirm Email</a></p>"
            };

            await _resend.EmailSendAsync(message);
        }
    }
}
