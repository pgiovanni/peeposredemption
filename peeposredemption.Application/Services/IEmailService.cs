namespace peeposredemption.Application.Services
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string toEmail, string confirmationLink);
        Task SendMaliciousLinkAlertAsync(string fromUsername, Guid channelId, string content);
    }
}
