using Microsoft.Extensions.Configuration;
using Resend;

namespace peeposredemption.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly string _fromAddress;
        private readonly string _adminEmail;
        private readonly string _leadsEmail;

        public EmailService(IResend resend, IConfiguration config)
        {
            _resend = resend;
            _fromAddress = config["Email:From"] ?? "noreply@torvex.app";
            _adminEmail = config["Email:AdminEmail"] ?? "pgiovanni1234@gmail.com";
            _leadsEmail = config["Email:LeadsEmail"] ?? "admin@torvex.app";
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

        public async Task SendReferralPurchaseNotificationAsync(string marketerUsername, string buyerUsername, long amountCents)
        {
            var amount = (amountCents / 100m).ToString("F2");
            var commission = (amountCents * 0.20m / 100m).ToString("F2");
            var message = new EmailMessage
            {
                From = $"PeePo's Redemption <{_fromAddress}>",
                To = { _adminEmail },
                Subject = $"[Torvex] Referral purchase — ${amount} by {buyerUsername}",
                HtmlBody = $"<p><strong>{buyerUsername}</strong> just made a purchase of <strong>${amount}</strong>.</p>" +
                           $"<p>Referred by: <strong>{marketerUsername}</strong></p>" +
                           $"<p>Commission owed: <strong>${commission}</strong> (20%)</p>" +
                           $"<p>View all payouts at <a href=\"https://torvex.app/App/Admin/Referrals\">Admin Referrals</a>.</p>"
            };
            await _resend.EmailSendAsync(message);
        }

        public async Task SendArtistSubmissionNotificationAsync(string displayName, string email, string portfolioUrl)
        {
            var message = new EmailMessage
            {
                From = $"PeePo's Redemption <{_fromAddress}>",
                To = { _adminEmail },
                Subject = $"[Torvex] New artist application — {displayName}",
                HtmlBody = $"<p>A new artist application has been submitted.</p>" +
                           $"<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(displayName)}<br/>" +
                           $"<strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}<br/>" +
                           $"<strong>Portfolio:</strong> <a href=\"{System.Net.WebUtility.HtmlEncode(portfolioUrl)}\">{System.Net.WebUtility.HtmlEncode(portfolioUrl)}</a></p>" +
                           $"<p>Review at <a href=\"https://admin.torvex.app/App/Admin/ArtistSubmissions\">Artist Submissions</a>.</p>"
            };
            await _resend.EmailSendAsync(message);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var message = new EmailMessage
            {
                From = $"PeePo's Redemption <{_fromAddress}>",
                To = { toEmail },
                Subject = "Reset your password",
                HtmlBody = $"<p>Click the link below to reset your password. This link expires in 30 minutes.</p><p><a href=\"{resetLink}\">Reset Password</a></p>"
            };

            await _resend.EmailSendAsync(message);
        }

        public async Task SendSupportTicketNotificationAsync(string username, string category, string subject, string description)
        {
            var message = new EmailMessage
            {
                From = $"PeePo's Redemption <{_fromAddress}>",
                To = { _adminEmail },
                Subject = $"[Torvex] New support ticket — [{category}] {subject}",
                HtmlBody = $"<p>A new support ticket has been submitted.</p>" +
                           $"<p><strong>User:</strong> {System.Net.WebUtility.HtmlEncode(username)}<br/>" +
                           $"<strong>Category:</strong> {System.Net.WebUtility.HtmlEncode(category)}<br/>" +
                           $"<strong>Subject:</strong> {System.Net.WebUtility.HtmlEncode(subject)}<br/>" +
                           $"<strong>Description:</strong> {System.Net.WebUtility.HtmlEncode(description)}</p>" +
                           $"<p>Review at <a href=\"https://torvex.app/App/Admin/SupportTicketAdmin\">Support Tickets Admin</a>.</p>"
            };
            await _resend.EmailSendAsync(message);
        }

        public async Task SendLeadNotificationAsync(string name, string email, string? company, string? package, string messageBody)
        {
            var subjectTag = string.IsNullOrWhiteSpace(package) ? "New inquiry" : $"ORDER: {package}";
            var message = new EmailMessage
            {
                From = $"Torvex <{_fromAddress}>",
                To = { _leadsEmail },
                Subject = $"[Torvex] {subjectTag} — {System.Net.WebUtility.HtmlEncode(name)}",
                HtmlBody = $"<p>A new inquiry came in through torvex.app/Contact.</p>" +
                           $"<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(name)}<br/>" +
                           $"<strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}<br/>" +
                           (string.IsNullOrWhiteSpace(company) ? "" : $"<strong>Company:</strong> {System.Net.WebUtility.HtmlEncode(company)}<br/>") +
                           (string.IsNullOrWhiteSpace(package) ? "" : $"<strong>Package:</strong> {System.Net.WebUtility.HtmlEncode(package)}<br/>") +
                           $"<strong>Message:</strong></p>" +
                           $"<p style=\"white-space:pre-wrap\">{System.Net.WebUtility.HtmlEncode(messageBody)}</p>" +
                           $"<p>Manage leads at <a href=\"https://admin.torvex.app/App/Admin/Leads\">Leads Admin</a>. Reply directly to the sender at {System.Net.WebUtility.HtmlEncode(email)}.</p>"
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
