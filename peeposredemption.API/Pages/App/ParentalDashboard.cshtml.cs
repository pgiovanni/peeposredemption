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

public class ParentalDashboardModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    public ParentalDashboardModel(IMediator mediator, IUnitOfWork uow) { _mediator = mediator; _uow = uow; }

    public ServerListViewModel ServerList { get; set; } = new();
    public List<ParentalDashboardChildDto> Children { get; set; } = new();
    [BindProperty] public string? ClaimCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return RedirectToPage("/Auth/Login");
        var userId = Guid.Parse(userIdClaim);

        await LoadServerList(userId);
        Children = await _mediator.Send(new GetParentalDashboardQuery(userId));

        return Page();
    }

    public async Task<IActionResult> OnPostClaimAsync()
    {
        var userId = GetUserId();
        await LoadServerList(userId);

        if (string.IsNullOrWhiteSpace(ClaimCode))
        {
            ErrorMessage = "Please enter a link code.";
            Children = await _mediator.Send(new GetParentalDashboardQuery(userId));
            return Page();
        }

        try
        {
            await _mediator.Send(new ClaimParentalLinkCommand(userId, ClaimCode));
            SuccessMessage = "Successfully linked! You can now manage parental controls.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        Children = await _mediator.Send(new GetParentalDashboardQuery(userId));
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateControlsAsync(Guid linkId, bool accountFrozen, bool dmFriendsOnly)
    {
        var userId = GetUserId();
        try
        {
            await _mediator.Send(new UpdateParentalControlsCommand(userId, linkId, accountFrozen, dmFriendsOnly));
            SuccessMessage = "Controls updated.";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
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
