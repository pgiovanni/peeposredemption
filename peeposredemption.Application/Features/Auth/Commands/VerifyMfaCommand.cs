using MediatR;
using OtpNet;
using peeposredemption.Application.DTOs.Auth;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace peeposredemption.Application.Features.Auth.Commands;

public record VerifyMfaCommand(
    string MfaPendingToken,
    string Code,
    string? IpAddress = null,
    string? UserAgent = null,
    Guid? DeviceId = null) : IRequest<LoginResultDto>;

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, LoginResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly TokenService _tokenService;

    public VerifyMfaCommandHandler(IUnitOfWork uow, TokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    public async Task<LoginResultDto> Handle(VerifyMfaCommand cmd, CancellationToken ct)
    {
        var principal = _tokenService.ValidateMfaPendingToken(cmd.MfaPendingToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired MFA session.");

        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Invalid MFA token.");

        var userId = Guid.Parse(userIdStr);
        var user = await _uow.Users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsMfaEnabled || string.IsNullOrEmpty(user.TotpSecret))
            throw new UnauthorizedAccessException("MFA is not enabled for this account.");

        var code = cmd.Code.Trim();
        var verified = false;

        // Try TOTP code first (6 digits)
        if (code.Length == 6 && code.All(char.IsDigit))
        {
            var secretBytes = Base32Encoding.ToBytes(user.TotpSecret);
            var totp = new Totp(secretBytes);
            verified = totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }

        // If not a valid TOTP, try recovery code (skip if input looks like a TOTP — avoids slow BCrypt on mistyped codes)
        var looksLikeTotp = code.Length == 6 && code.All(char.IsDigit);
        if (!verified && !looksLikeTotp && !string.IsNullOrEmpty(user.MfaRecoveryCodes))
        {
            var hashedCodes = JsonSerializer.Deserialize<List<string>>(user.MfaRecoveryCodes) ?? new();
            var matchIndex = hashedCodes.FindIndex(h => BCrypt.Net.BCrypt.Verify(code, h));

            if (matchIndex >= 0)
            {
                verified = true;
                hashedCodes.RemoveAt(matchIndex);
                user.MfaRecoveryCodes = JsonSerializer.Serialize(hashedCodes);
                if (hashedCodes.Count == 0)
                {
                    user.IsMfaEnabled = false;
                    user.TotpSecret = null;
                    user.MfaRecoveryCodes = null;
                }
            }
        }

        if (!verified)
            throw new UnauthorizedAccessException("Invalid verification code.");

        // Issue full auth tokens
        var jwt = _tokenService.GenerateToken(user);
        var rawRefresh = _tokenService.GenerateRefreshToken();

        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = TokenService.HashToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IpAddress = cmd.IpAddress,
            UserAgent = cmd.UserAgent,
            DeviceId = cmd.DeviceId
        });
        await _uow.SaveChangesAsync();

        return LoginResultDtoExtensions.FullLogin(jwt, rawRefresh, user.Id);
    }
}
