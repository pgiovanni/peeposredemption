using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Auth.Commands;
using peeposredemption.Application.Features.Auth.Queries;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

[Authorize]
public class MfaSetupModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;

    public MfaSetupModel(IMediator mediator, IUnitOfWork uow)
    {
        _mediator = mediator;
        _uow = uow;
    }

    public ServerListViewModel ServerList { get; set; } = new();
    public bool IsMfaEnabled { get; set; }

    // Setup flow
    public string? QrCodeBase64 { get; set; }
    public string? Secret { get; set; }
    [BindProperty] public string? SetupCode { get; set; }
    [BindProperty] public string? SetupSecret { get; set; }

    // Disable flow
    [BindProperty] public string? DisableCode { get; set; }

    // After confirm
    public List<string>? RecoveryCodes { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadServerListAsync(userId.Value);
        var user = await _uow.Users.GetByIdAsync(userId.Value);
        IsMfaEnabled = user?.IsMfaEnabled ?? false;

        if (!IsMfaEnabled)
        {
            var setup = await _mediator.Send(new GenerateMfaSetupQuery(userId.Value));
            QrCodeBase64 = setup.QrCodeBase64;
            Secret = setup.Secret;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadServerListAsync(userId.Value);

        if (string.IsNullOrWhiteSpace(SetupCode) || string.IsNullOrWhiteSpace(SetupSecret))
        {
            ErrorMessage = "Please enter the verification code.";
            var setup = await _mediator.Send(new GenerateMfaSetupQuery(userId.Value));
            QrCodeBase64 = setup.QrCodeBase64;
            Secret = setup.Secret;
            return Page();
        }

        try
        {
            RecoveryCodes = await _mediator.Send(
                new ConfirmMfaSetupCommand(userId.Value, SetupSecret, SetupCode));
            IsMfaEnabled = true;
            SuccessMessage = "Two-factor authentication has been enabled!";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            // Re-generate QR for retry
            var setup = await _mediator.Send(new GenerateMfaSetupQuery(userId.Value));
            QrCodeBase64 = setup.QrCodeBase64;
            Secret = setup.Secret;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToPage("/Auth/Login");

        await LoadServerListAsync(userId.Value);

        if (string.IsNullOrWhiteSpace(DisableCode))
        {
            ErrorMessage = "Please enter your verification code.";
            IsMfaEnabled = true;
            return Page();
        }

        try
        {
            await _mediator.Send(new DisableMfaCommand(userId.Value, DisableCode));
            SuccessMessage = "Two-factor authentication has been disabled.";
            IsMfaEnabled = false;
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            IsMfaEnabled = true;
        }

        return Page();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim == null ? null : Guid.Parse(claim);
    }

    private async Task LoadServerListAsync(Guid userId)
    {
        var servers = await _mediator.Send(new GetUserServersQuery(userId));
        var defaultChannels = new Dictionary<Guid, Guid>();
        foreach (var s in servers)
        {
            var chs = await _mediator.Send(new GetServerChannelsQuery(s.Id));
            var first = chs.FirstOrDefault();
            if (first != null) defaultChannels[s.Id] = first.Id;
        }
        var unreadDms = await _uow.DirectMessages.GetUnreadCountAsync(userId);
        var unreadPings = await _uow.Notifications.GetUnreadCountAsync(userId);
        var serverUnreadCounts = await _uow.Notifications.GetUnreadCountByServerAsync(userId);
        var dmUnreadCounts = await _uow.DirectMessages.GetUnreadCountBySenderAsync(userId);
        ServerList = new ServerListViewModel
        {
            Servers = servers,
            ServerDefaultChannels = defaultChannels,
            UnreadCount = unreadDms + unreadPings,
            ServerUnreadCounts = serverUnreadCounts,
            DmUnreadCounts = dmUnreadCounts
        };
    }
}
