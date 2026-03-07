using MediatR;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Auth.Commands
{
    public record ResendConfirmationCommand(string Email) : IRequest<Unit>;

    public class ResendConfirmationCommandHandler : IRequestHandler<ResendConfirmationCommand, Unit>
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;

        public ResendConfirmationCommandHandler(IUnitOfWork uow, IEmailService emailService)
        { _uow = uow; _emailService = emailService; }

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

            var confirmationLink = $"https://localhost:443/Auth/Confirm?token={user.EmailConfirmationtoken}";
            _ = _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink);

            return Unit.Value;
        }
    }
}
