using MediatR;
using peeposredemption.Application.DTOs.Auth;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Auth.Commands;

/// <summary>
/// "Sign in with Discord". Identity comes from a completed OAuth2 code exchange
/// (the caller has already talked to Discord). Resolution order:
///   1. an existing DiscordLink for this Discord id → that account;
///   2. else a Torvex account whose email equals Discord's VERIFIED email → link it;
///   3. else create a fresh account (confirmed iff Discord verified the email).
/// Accounts the bot auto-created (discord_*@bot.torvex.app) get their real
/// email filled in when Discord vouches for it, so invoices can reach them.
/// </summary>
public record DiscordLoginCommand(
    string DiscordUserId,
    string Username,
    string? GlobalName,
    string? Email,
    bool EmailVerified,
    string? AvatarUrl,
    string? IpAddress = null,
    string? UserAgent = null,
    Guid? DeviceId = null) : IRequest<LoginResultDto>;

public class DiscordLoginCommandHandler : IRequestHandler<DiscordLoginCommand, LoginResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly TokenService _tokenService;

    public DiscordLoginCommandHandler(IUnitOfWork uow, TokenService tokenService)
    {
        _uow = uow;
        _tokenService = tokenService;
    }

    public async Task<LoginResultDto> Handle(DiscordLoginCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.DiscordUserId) || !cmd.DiscordUserId.All(char.IsDigit))
            throw new UnauthorizedAccessException("Discord didn't return a valid account.");

        var verifiedEmail = cmd.EmailVerified && !string.IsNullOrWhiteSpace(cmd.Email) ? cmd.Email!.Trim() : null;

        User? user = null;
        var link = await _uow.DiscordLinks.GetByDiscordIdAsync(cmd.DiscordUserId);
        if (link != null)
        {
            user = await _uow.Users.GetByIdAsync(link.TorvexUserId);
            if (user != null && verifiedEmail != null
                && user.Email.EndsWith("@bot.torvex.app", StringComparison.OrdinalIgnoreCase)
                && !await _uow.Users.EmailExistsAsync(verifiedEmail))
            {
                user.Email = verifiedEmail;
                user.EmailConfirmed = true;
            }
        }

        if (user == null && verifiedEmail != null)
        {
            user = await _uow.Users.GetByEmailAsync(verifiedEmail);
            if (user != null)
                await _uow.DiscordLinks.AddAsync(new DiscordLink { DiscordUserId = cmd.DiscordUserId, TorvexUserId = user.Id });
        }

        if (user == null)
        {
            if (verifiedEmail == null)
                throw new UnauthorizedAccessException("Your Discord account needs a verified email to sign in here.");

            var username = await UniqueUsernameAsync(cmd.Username);
            user = new User
            {
                Username = username,
                DisplayName = string.IsNullOrWhiteSpace(cmd.GlobalName) ? null : cmd.GlobalName.Trim(),
                Email = verifiedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                EmailConfirmed = true,
                AvatarUrl = cmd.AvatarUrl
            };
            await _uow.Users.AddAsync(user);
            await _uow.DiscordLinks.AddAsync(new DiscordLink { DiscordUserId = cmd.DiscordUserId, TorvexUserId = user.Id });
        }

        if (!user.EmailConfirmed)
            throw new UnauthorizedAccessException("Please confirm your email before logging in.");

        if (user.IsMfaEnabled)
        {
            await _uow.SaveChangesAsync();
            return LoginResultDtoExtensions.MfaPending(_tokenService.GenerateMfaPendingToken(user), user.Id);
        }

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

    private async Task<string> UniqueUsernameAsync(string discordUsername)
    {
        var baseName = new string((discordUsername ?? "user").Where(c => char.IsLetterOrDigit(c) || c is '_' or '.').ToArray());
        if (baseName.Length < 3) baseName = "discord_" + baseName;
        baseName = baseName[..Math.Min(baseName.Length, 28)];
        var candidate = baseName;
        var n = 1;
        while (await _uow.Users.UsernameExistsAsync(candidate))
            candidate = $"{baseName[..Math.Min(baseName.Length, 24)]}_{n++}";
        return candidate;
    }
}
