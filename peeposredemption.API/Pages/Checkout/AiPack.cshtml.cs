using System.Net.Http.Json;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.API.Infrastructure;
using peeposredemption.Application.Features.Shop.Commands;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.API.Pages.Checkout;

/// <summary>
/// Cart for the Discord AI credit pack. Entered from the bot dashboard with the
/// chosen server (?guild=&name=), or bare — in which case we send the visitor
/// to the dashboard's server picker, which bounces back here with the server.
/// </summary>
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AiPackModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<AiPackModel> _logger;
    private readonly IHttpClientFactory _http;

    public AiPackModel(IMediator mediator, IUnitOfWork uow, IConfiguration config,
                       ILogger<AiPackModel> logger, IHttpClientFactory http)
    {
        _mediator = mediator;
        _uow = uow;
        _config = config;
        _logger = logger;
        _http = http;
    }

    public string? GuildId { get; private set; }
    public string? GuildName { get; private set; }
    public string? GuildIconUrl { get; private set; }
    /// <summary>true/false from the bot dashboard's verified lookup; null when the lookup was unreachable.</summary>
    public bool? BotInGuild { get; private set; }
    public string Email { get; private set; } = "";
    public string? Error { get; private set; }

    private sealed record GuildCard(bool in_guild, string? name, string? icon_url);

    /// <summary>
    /// Verify the server against the bot dashboard rather than trusting the
    /// query string: the buyer must see the REAL name and icon of the guild id
    /// they're paying for, and be stopped if the bot isn't in it at all.
    /// Lookup failure degrades to the passed name — an internal hiccup should
    /// not block a sale, only remove the verification.
    /// </summary>
    private async Task ResolveGuildCardAsync(string gid, string? fallbackName)
    {
        try
        {
            var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(4);
            var dashUrl = (_config["Forerunner:DashboardUrl"] ?? "https://forerunner.torvex.app").TrimEnd('/');
            var card = await client.GetFromJsonAsync<GuildCard>($"{dashUrl}/api/guild-card/{gid}");
            if (card != null)
            {
                BotInGuild = card.in_guild;
                GuildName = CleanGuildName(card.name) ?? fallbackName;
                GuildIconUrl = card.icon_url;
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guild card lookup failed for {Guild}", gid);
        }
        BotInGuild = null;
        GuildName = fallbackName;
    }

    /// <summary>Bot-dashboard server picker; it returns to this page with ?guild=&name=.</summary>
    public string PickServerUrl =>
        (_config["Forerunner:DashboardUrl"] ?? "https://forerunner.torvex.app").TrimEnd('/') + "/buy-ai";

    private static string? CleanGuildId(string? raw)
    {
        raw = raw?.Trim();
        // Discord snowflakes: 17–20 digits.
        return raw != null && raw.Length is >= 17 and <= 20 && raw.All(char.IsDigit) ? raw : null;
    }

    private static string? CleanGuildName(string? raw)
    {
        raw = raw?.Trim();
        if (string.IsNullOrEmpty(raw)) return null;
        return raw.Length > 100 ? raw[..100] : raw;
    }

    public async Task<IActionResult> OnGetAsync(string? guild = null, string? name = null, string? error = null)
    {
        var userId = GetUserId();
        if (userId == null)
            return Redirect("/Auth/Login?returnUrl=" + Uri.EscapeDataString(Request.Path + Request.QueryString));

        var user = await _uow.Users.GetByIdAsync(userId.Value);
        if (user == null) return RedirectToPage("/Auth/Login");
        Email = user.Email;

        GuildId = CleanGuildId(guild);
        if (GuildId != null)
            await ResolveGuildCardAsync(GuildId, CleanGuildName(name));
        if (guild != null && GuildId == null) Error = "That server id doesn't look right — pick the server again.";
        if (error == "email") Error = "Add a real email address to your account (Dashboard → Security) before checking out — invoices are emailed.";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? guildId, string? guildName)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        var gid = CleanGuildId(guildId);
        if (gid == null) return RedirectToPage("/Checkout/AiPack", new { error = "guild" });

        // Re-verify at payment time too, so the Stripe description and the
        // order record carry the REAL server name — and a pack can't be bought
        // for a server the bot verifiably isn't in.
        await ResolveGuildCardAsync(gid, CleanGuildName(guildName));
        if (BotInGuild == false)
            return RedirectToPage("/Checkout/AiPack", new { guild = gid, name = guildName });

        var pkg = ServiceCatalog.AiPack;
        var baseUrl = _config["AppBaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        try
        {
            var url = await _mediator.Send(new CreatePackageOrderSessionCommand(
                userId.Value,
                ServiceCatalog.AiPackSlug,
                pkg.Name,
                $"Prepaid AI credit pack: ${ServiceCatalog.AiPackCreditUsd:0.00} of AI usage for your Discord server, no expiry.",
                ServiceCatalog.AiPackPriceCents,
                gid,
                GuildName ?? CleanGuildName(guildName),
                ServiceCatalog.AiPackCreditUsd,
                baseUrl));
            return Redirect(url);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("real email"))
        {
            return RedirectToPage("/Checkout/AiPack", new { guild = gid, name = guildName, error = "email" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI pack checkout failed for {UserId} guild {Guild}", userId, gid);
            Email = (await _uow.Users.GetByIdAsync(userId.Value))?.Email ?? "";
            GuildId = gid; GuildName = CleanGuildName(guildName);
            Error = "Couldn't start checkout right now. Please try again in a minute.";
            return Page();
        }
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
