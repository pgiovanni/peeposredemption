using MediatR;
using OtpNet;
using peeposredemption.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;

namespace peeposredemption.Application.Features.Auth.Commands;

public record ConfirmMfaSetupCommand(Guid UserId, string Secret, string Code) : IRequest<List<string>>;

public class ConfirmMfaSetupCommandHandler : IRequestHandler<ConfirmMfaSetupCommand, List<string>>
{
    private readonly IUnitOfWork _uow;

    public ConfirmMfaSetupCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<string>> Handle(ConfirmMfaSetupCommand cmd, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(cmd.UserId)
            ?? throw new InvalidOperationException("User not found.");

        // Validate the TOTP code against the provided secret
        var secretBytes = Base32Encoding.ToBytes(cmd.Secret);
        var totp = new Totp(secretBytes);
        if (!totp.VerifyTotp(cmd.Code.Trim(), out _, new VerificationWindow(previous: 1, future: 1)))
            throw new InvalidOperationException("Invalid verification code. Please try again.");

        // Generate 8 recovery codes
        var recoveryCodes = new List<string>();
        var hashedCodes = new List<string>();
        for (int i = 0; i < 8; i++)
        {
            var code = GenerateRecoveryCode();
            recoveryCodes.Add(code);
            hashedCodes.Add(BCrypt.Net.BCrypt.HashPassword(code));
        }

        // Save to user
        user.TotpSecret = cmd.Secret;
        user.IsMfaEnabled = true;
        user.MfaRecoveryCodes = JsonSerializer.Serialize(hashedCodes);
        await _uow.SaveChangesAsync();

        return recoveryCodes;
    }

    private static string GenerateRecoveryCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(5);
        var hex = Convert.ToHexString(bytes).ToLower();
        return $"{hex[..5]}-{hex[5..]}";
    }
}
