using MediatR;
using peeposredemption.Application.DTOs.Auth;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? UserAgent = null,
    Guid? DeviceId = null) : IRequest<LoginResultDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly TokenService _tokenService;

    public RefreshTokenCommandHandler(IUnitOfWork uow, TokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    public async Task<LoginResultDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = TokenService.HashToken(cmd.RefreshToken);
        var existing = await _uow.RefreshTokens.GetByTokenHashAsync(hash)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (existing.IsRevoked)
        {
            // If this token was revoked because it was rotated (has a replacement),
            // this is likely a concurrent request that lost the race — not actual theft.
            // Find the replacement chain and return its JWT instead of nuking everything.
            if (!string.IsNullOrEmpty(existing.ReplacedByTokenId))
            {
                var replacement = await _uow.RefreshTokens.GetByIdAsync(
                    Guid.Parse(existing.ReplacedByTokenId));
                if (replacement != null && !replacement.IsRevoked && replacement.ExpiresAt > DateTime.UtcNow)
                {
                    var user2 = await _uow.Users.GetByIdAsync(existing.UserId)
                        ?? throw new UnauthorizedAccessException("User not found.");
                    var jwt2 = _tokenService.GenerateToken(user2);
                    // Return same refresh token (the replacement) — don't rotate again
                    return LoginResultDtoExtensions.FullLogin(jwt2, cmd.RefreshToken, user2.Id);
                }
            }

            // Token was revoked WITHOUT a replacement — genuine reuse / theft
            await _uow.RefreshTokens.RevokeAllForUserAsync(existing.UserId);
            await _uow.SaveChangesAsync();
            throw new UnauthorizedAccessException("Token reuse detected. All sessions revoked.");
        }

        if (existing.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");

        existing.IsRevoked = true;

        var user = await _uow.Users.GetByIdAsync(existing.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var newJwt = _tokenService.GenerateToken(user);
        var newRawRefresh = _tokenService.GenerateRefreshToken();

        var newRefreshEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = TokenService.HashToken(newRawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IpAddress = cmd.IpAddress ?? existing.IpAddress,
            UserAgent = cmd.UserAgent ?? existing.UserAgent,
            DeviceId = cmd.DeviceId ?? existing.DeviceId
        };
        existing.ReplacedByTokenId = newRefreshEntity.Id.ToString();

        await _uow.RefreshTokens.AddAsync(newRefreshEntity);
        await _uow.SaveChangesAsync();

        return LoginResultDtoExtensions.FullLogin(newJwt, newRawRefresh, user.Id);
    }
}
