using MediatR;
using OtpNet;
using peeposredemption.Domain.Interfaces;
using System.Text.Json;

namespace peeposredemption.Application.Features.Auth.Commands;

public record DisableMfaCommand(Guid UserId, string Code) : IRequest;

public class DisableMfaCommandHandler : IRequestHandler<DisableMfaCommand>
{
    private readonly IUnitOfWork _uow;

    public DisableMfaCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DisableMfaCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(cmd.UserId)
            ?? throw new InvalidOperationException("User not found.");

        if (!user.IsMfaEnabled || string.IsNullOrEmpty(user.TotpSecret))
            throw new InvalidOperationException("MFA is not enabled.");

        var code = cmd.Code.Trim();
        var verified = false;

        // Try TOTP
        if (code.Length == 6 && code.All(char.IsDigit))
        {
            var secretBytes = Base32Encoding.ToBytes(user.TotpSecret);
            var totp = new Totp(secretBytes);
            verified = totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }

        // Try recovery code
        if (!verified && !string.IsNullOrEmpty(user.MfaRecoveryCodes))
        {
            var hashedCodes = JsonSerializer.Deserialize<List<string>>(user.MfaRecoveryCodes) ?? new();
            verified = hashedCodes.Any(h => BCrypt.Net.BCrypt.Verify(code, h));
        }

        if (!verified)
            throw new InvalidOperationException("Invalid verification code.");

        user.IsMfaEnabled = false;
        user.TotpSecret = null;
        user.MfaRecoveryCodes = null;
        await _uow.SaveChangesAsync();
    }
}
