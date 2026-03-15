using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Application.Features.Channels.Queries;
using peeposredemption.Application.Features.ParentalControls.Commands;
using peeposredemption.Application.Features.ParentalControls.Queries;
using peeposredemption.Application.Features.Servers.Queries;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App;

public class ParentalLinkModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    public ParentalLinkModel(IMediator mediator, IUnitOfWork uow) { _mediator = mediator; _uow = uow; }

    public ServerListViewModel ServerList { get; set; } = new();
    public MyParentalLinkDto? ActiveLink { get; set; }
    public string? PendingCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool IsMinor { get; set; }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);

        IsMinor = User.FindFirst("IsMinor")?.Value == "true";
        await LoadServerList(userId);
        ActiveLink = await _mediator.Send(new GetMyParentalLinkQuery(userId));

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateCodeAsync()
    {
        var userId = GetUserId();
        IsMinor = User.FindFirst("IsMinor")?.Value == "true";
        await LoadServerList(userId);

        try
        {
            PendingCode = await _mediator.Send(new GenerateParentalLinkCodeCommand(userId));
            SuccessMessage = "Share this code with your parent/guardian.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        ActiveLink = await _mediator.Send(new GetMyParentalLinkQuery(userId));
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid linkId)
    {
        var userId = GetUserId();
        try
        {
            await _mediator.Send(new RevokeParentalLinkCommand(userId, linkId));
            SuccessMessage = "Parental link has been revoked.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadServerList(Guid userId)
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
