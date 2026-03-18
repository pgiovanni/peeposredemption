using MediatR;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Auth.Commands
{
    public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IUnitOfWork _uow;

        public ResetPasswordCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(ResetPasswordCommand cmd, CancellationToken ct)
        {
            var user = await _uow.Users.GetByPasswordResetTokenAsync(cmd.Token)
                ?? throw new InvalidOperationException("Invalid or expired reset link.");

            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Invalid or expired reset link.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await _uow.SaveChangesAsync();
        }
    }
}
