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

    public AiPackModel(IMediator mediator, IUnitOfWork uow, IConfiguration config, ILogger<AiPackModel> logger)
    {
        _mediator = mediator;
        _uow = uow;
        _config = config;
        _logger = logger;
    }

    public string? GuildId { get; private set; }
    public string? GuildName { get; private set; }
    public string Email { get; private set; } = "";
    public string? Error { get; private set; }

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
        GuildName = GuildId != null ? CleanGuildName(name) : null;
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
                CleanGuildName(guildName),
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
