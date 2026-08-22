using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace peeposredemption.API.Infrastructure;

/// <summary>
/// Minimal Discord OAuth2 (authorization-code) helper for "Sign in with Discord".
/// Scopes are identity-only (identify + email) — no guilds, no bot permissions;
/// anything guild-related is checked with the bot token elsewhere.
/// </summary>
public static class DiscordOAuth
{
    public const string StateCookie = "discord_oauth_state";

    public record DiscordUser(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("global_name")] string? GlobalName,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("verified")] bool? Verified,
        [property: JsonPropertyName("avatar")] string? Avatar);

    public static bool IsConfigured(IConfiguration cfg) =>
        !string.IsNullOrEmpty(cfg["Discord:ClientId"]) && !string.IsNullOrEmpty(cfg["Discord:ClientSecret"]);

    public static string RedirectUri(IConfiguration cfg) =>
        cfg["Discord:RedirectUri"]
        ?? (cfg["AppBaseUrl"]?.TrimEnd('/') ?? "https://torvex.app") + "/Auth/Discord/Callback";

    public static string NewState() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string AuthorizeUrl(IConfiguration cfg, string state) =>
        "https://discord.com/oauth2/authorize"
        + "?client_id=" + Uri.EscapeDataString(cfg["Discord:ClientId"]!)
        + "&redirect_uri=" + Uri.EscapeDataString(RedirectUri(cfg))
        + "&response_type=code&scope=identify%20email&prompt=none"
        + "&state=" + Uri.EscapeDataString(state);

    /// <summary>Only same-site relative paths are honoured as post-login targets.</summary>
    public static string SafeReturnUrl(string? url, string fallback) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.Contains('\\')
            ? url : fallback;

    public static async Task<DiscordUser?> ExchangeAsync(IConfiguration cfg, IHttpClientFactory http, string code)
    {
        var client = http.CreateClient();
        using var tokenRes = await client.PostAsync("https://discord.com/api/v10/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = cfg["Discord:ClientId"]!,
                ["client_secret"] = cfg["Discord:ClientSecret"]!,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = RedirectUri(cfg)
            }));
        if (!tokenRes.IsSuccessStatusCode) return null;
        using var tokenDoc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
        var access = tokenDoc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        if (string.IsNullOrEmpty(access)) return null;

        using var req = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        using var meRes = await client.SendAsync(req);
        if (!meRes.IsSuccessStatusCode) return null;
        return JsonSerializer.Deserialize<DiscordUser>(await meRes.Content.ReadAsStringAsync());
    }

    public static string? AvatarUrl(DiscordUser u) =>
        string.IsNullOrEmpty(u.Avatar) ? null
            : $"https://cdn.discordapp.com/avatars/{u.Id}/{u.Avatar}.{(u.Avatar.StartsWith("a_") ? "gif" : "png")}?size=256";
}
