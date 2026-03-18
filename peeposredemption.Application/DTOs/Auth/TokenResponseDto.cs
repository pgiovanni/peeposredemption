using System;

namespace peeposredemption.Application.DTOs.Auth
{
    public record LoginResultDto(
        bool RequiresMfa,
        string? MfaPendingToken,
        string? Token,
        string? RefreshToken,
        Guid UserId);

    // Keep backward compat alias for minimal changes in refresh flow
    public static class LoginResultDtoExtensions
    {
        public static LoginResultDto FullLogin(string token, string refreshToken, Guid userId)
            => new(false, null, token, refreshToken, userId);

        public static LoginResultDto MfaPending(string pendingToken, Guid userId)
            => new(true, pendingToken, null, null, userId);
    }
}
