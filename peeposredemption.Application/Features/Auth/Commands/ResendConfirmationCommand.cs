using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Auth.Commands
{
    public record ResendConfirmationCommand(string Email) : IRequest<Unit>;

    public class ResendConfirmationCommandHandler : IRequestHandler<ResendConfirmationCommand, Unit>
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<ResendConfirmationCommandHandler> _logger;

        public ResendConfirmationCommandHandler(IUnitOfWork uow, IEmailService emailService, IConfiguration config, ILogger<ResendConfirmationCommandHandler> logger)
        { _uow = uow; _emailService = emailService; _config = config; _logger = logger; }

        public async Task<Unit> Handle(ResendConfirmationCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByEmailAsync(cmd.Email);

            // Silently succeed if user not found or already confirmed (don't leak info)
            if (user is null || user.EmailConfirmed)
                return Unit.Value;

            // Regenerate token in case the old one was cleared
            if (string.IsNullOrEmpty(user.EmailConfirmationtoken))
            {
                user.EmailConfirmationtoken = Guid.NewGuid().ToString();
                await _uow.SaveChangesAsync();
            }

            var baseUrl = _config["AppBaseUrl"] ?? "https://localhost:443";
            var confirmationLink = $"{baseUrl}/Auth/ConfirmEmail?token={user.EmailConfirmationtoken}";
            _logger.LogInformation("Sending confirmation email to {Email} via {BaseUrl}", user.Email, baseUrl);
            await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink);

            return Unit.Value;
        }
    }
}
