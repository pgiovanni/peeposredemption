using MediatR;
using peeposredemption.Application.DTOs.Auth;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponseDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponseDto>
{
    private readonly IUnitOfWork _uow;
    private readonly TokenService _tokenService;

    public RefreshTokenCommandHandler(IUnitOfWork uow, TokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    public async Task<TokenResponseDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = TokenService.HashToken(cmd.RefreshToken);
        var existing = await _uow.RefreshTokens.GetByTokenHashAsync(hash)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (existing.IsRevoked)
        {
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
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        existing.ReplacedByTokenId = newRefreshEntity.Id.ToString();

        await _uow.RefreshTokens.AddAsync(newRefreshEntity);
        await _uow.SaveChangesAsync();

        return new TokenResponseDto(newJwt, newRawRefresh);
    }
}
