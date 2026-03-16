namespace peeposredemption.Application.Services
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string toEmail, string confirmationLink);
        Task SendMaliciousLinkAlertAsync(string fromUsername, Guid channelId, string content);
        Task SendNewUserNotificationAsync(string username, string email);
        Task SendReferralPurchaseNotificationAsync(string marketerUsername, string buyerUsername, long amountCents);
        Task SendArtistSubmissionNotificationAsync(string displayName, string email, string portfolioUrl);
    }
}
