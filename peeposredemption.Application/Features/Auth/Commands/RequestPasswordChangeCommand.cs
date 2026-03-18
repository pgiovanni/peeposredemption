using MediatR;
using Microsoft.Extensions.Configuration;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Auth.Commands
{
    public record RequestPasswordChangeCommand(Guid UserId) : IRequest;

    public class RequestPasswordChangeCommandHandler : IRequestHandler<RequestPasswordChangeCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public RequestPasswordChangeCommandHandler(IUnitOfWork uow, IEmailService emailService, IConfiguration config)
        {
            _uow = uow;
            _emailService = emailService;
            _config = config;
        }

        public async Task Handle(RequestPasswordChangeCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByIdAsync(cmd.UserId)
                ?? throw new InvalidOperationException("User not found.");

            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            await _uow.SaveChangesAsync();

            var baseUrl = _config["AppBaseUrl"] ?? "https://localhost:443";
            var resetLink = $"{baseUrl}/Auth/ResetPassword?token={token}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
        }
    }
}
