namespace peeposredemption.Application.Services
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string toEmail, string confirmationLink);
        Task SendMaliciousLinkAlertAsync(string fromUsername, Guid channelId, string content);
        Task SendNewUserNotificationAsync(string username, string email);
        Task SendReferralPurchaseNotificationAsync(string marketerUsername, string buyerUsername, long amountCents);
        Task SendArtistSubmissionNotificationAsync(string displayName, string email, string portfolioUrl);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendSupportTicketNotificationAsync(string username, string category, string subject, string description);
        Task SendLeadNotificationAsync(string name, string email, string? company, string? package, string message);
        Task SendOrderReceiptAsync(string toEmail, string customerName, string packageName, long amountCents,
            string? invoiceNumber, string? invoiceUrl, string? invoicePdfUrl, string? discordServer, decimal? creditUsd);
    }
}
